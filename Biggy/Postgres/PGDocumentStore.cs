using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using Biggy.Extensions;


namespace Biggy.Postgres {
  public class PGDocumentStore<T> : BiggyDocumentStore<T> where T : new() {
    public PGDocumentStore(DbCache context) : base(context) { }
    public PGDocumentStore(DbCache context, string tableName) : base(context, tableName) { }
    public PGDocumentStore(string connectionStringName) : base(new PGCache(connectionStringName)) { }
    public PGDocumentStore(string connectionStringName, string tableName) : base(new PGCache(connectionStringName), tableName) { }

    public override BiggyRelationalStore<dynamic> getModel() {
      return new PGStore<dynamic>(this.DbCache);
    }

    protected override List<T> TryLoadData() {
      try {
        return this.LoadAll();
      }
      catch (Npgsql.NpgsqlException x) {
        if (x.Message.Contains("does not exist")) {

          //create the table
          var sql = "";
          var keyColumnDefs = new List<string>();
          var keyColumnNames = new List<string>();
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search tsvector";
          }
          string lazyLoadingColumns = "";
          if (this.LazyLoadingFields.Length > 0) {
              lazyLoadingColumns = string.Format(", {0} json", string.Join(" json, ", this.LazyLoadingFields));
          }
          string keyDefStub = "{0} {1} not null";
          foreach (var pk in Model.TableMapping.PrimaryKeyMapping) {
            string integerKeyType = "integer";
            if (pk.IsAutoIncementing) {
              integerKeyType = "serial";
            }
            var pkType = pk.DataType == typeof(int) ? " " + integerKeyType : "varchar(255)";
            keyColumnDefs.Add(string.Format(keyDefStub, pk.DelimitedColumnName, pkType));
            keyColumnNames.Add(pk.DelimitedColumnName);
          }
          string keyColumnsStatement = string.Join(",", keyColumnDefs.ToArray());
          string pkColumns = string.Join(",", keyColumnNames.ToArray());
          sql = string.Format("CREATE TABLE {0} ({1}, body json not null {2} {3}, PRIMARY KEY ({4}));", this.TableMapping.DelimitedTableName, keyColumnsStatement, fullTextColumn, lazyLoadingColumns, pkColumns);
          this.Model.Execute(sql);
          return TryLoadData();
        } else {
          throw;
        }
      }
    }


    public override T Insert(T item) {
      this.addItem(item);
      // if there is a single, auto-incrementing pk:
      if (!this.TableMapping.HasCompoundPk && this.TableMapping.PrimaryKeyMapping[0].IsAutoIncementing) {
        // Sync the JSON ID with the serial PK:
        this.Update(item);
      }
      return item;
    }

    internal void addItem(T item) {
      var expando = base.SetDataForDocument(item);
      var dc = expando as IDictionary<string, object>;
      var vals = new List<string>();
      var args = new List<object>();
      var index = 0;

      var autoPkColumn = this.TableMapping.PrimaryKeyMapping.FirstOrDefault(c => c.IsAutoIncementing == true);
      if (autoPkColumn != null) {
        var keyColumn = dc.FirstOrDefault(x => x.Key.Equals(autoPkColumn.PropertyName, StringComparison.OrdinalIgnoreCase));
        //don't update the Primary Key
        dc.Remove(keyColumn);
      }
      var columnNames = new List<string>();
      foreach (var key in dc.Keys) {
        if (key == "search") {
          vals.Add(string.Format("to_tsvector(@{0})", index));
          columnNames.Add(this.TableMapping.ColumnMappings.FindByProperty(key).DelimitedColumnName);
        } else {
          vals.Add(string.Format("@{0}", index));
          columnNames.Add(this.TableMapping.ColumnMappings.FindByProperty(key).DelimitedColumnName);
        }
        args.Add(dc[key]);
        index++;
      }
      var sb = new StringBuilder();
      if (autoPkColumn != null) {
        sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}) RETURNING {3} as newID;", this.TableMapping.DelimitedTableName, string.Join(",", columnNames), string.Join(",", vals), autoPkColumn.DelimitedColumnName);
      } else {
        sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2});", this.TableMapping.DelimitedTableName, string.Join(",", columnNames), string.Join(",", vals));
      }
      var sql = sb.ToString();
      var newKey = this.Model.Scalar(sql, args.ToArray());
      //set the key
      if (autoPkColumn != null) {
        this.Model.SetPropertyValue(item, autoPkColumn.PropertyName, newKey);
      }
    }

    /// <summary>
    /// A high-performance bulk-insert that can drop 10,000 documents in about 900 ms
    /// </summary>
    public override List<T> BulkInsert(List<T> items) {
      const int MAGIC_PG_PARAMETER_LIMIT = 2100;
      const int MAGIC_PG_ROW_VALUE_LIMIT = 1000;
      int rowsAffected = 0;

      var first = items.First();
      string insertClause = "";
      var sbSql = new StringBuilder("");

      using (var connection = this.DbCache.OpenConnection()) {
        using (var tdbTransaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)) {
          var commands = new List<DbCommand>();
          // Lock the table, so nothing will disrupt the pk sequence:
          string lockTableSQL = string.Format("LOCK TABLE {0} in ACCESS EXCLUSIVE MODE", this.TableMapping.DelimitedTableName);
          DbCommand dbCommand = this.Model.CreateCommand(lockTableSQL, connection);
          dbCommand.Transaction = tdbTransaction;
          dbCommand.ExecuteNonQuery();
          var autoPkColumn = this.TableMapping.PrimaryKeyMapping.FirstOrDefault(c => c.IsAutoIncementing == true);

          int nextSerialPk = 0;
          if (autoPkColumn != null) {
            // Now get the next serial Id. ** Need to do this within the transaction/table lock scope **:
            string sequence = string.Format("\"{0}_{1}_seq\"", this.TableMapping.DBTableName, autoPkColumn.ColumnName);
            var sql_get_seq = string.Format("SELECT last_value FROM {0}", sequence);
            dbCommand.CommandText = sql_get_seq;
            // if this is a fresh sequence, the "seed" value is returned. We will assume 1:
            nextSerialPk = Convert.ToInt32(dbCommand.ExecuteScalar());
            // If this is not a fresh sequence, increment:
            if (nextSerialPk > 1) {
              nextSerialPk++;
            }
          }

          var paramCounter = 0;
          var rowValueCounter = 0;
          foreach (var item in items) {
            // Set the soon-to-be inserted serial int value:
            if (autoPkColumn != null) {
              var props = item.GetType().GetProperties();
              var pk = props.First(p => p.Name == autoPkColumn.PropertyName);
              pk.SetValue(item, nextSerialPk);
              nextSerialPk++;
            }
            // Set the JSON object, including the interpolated serial PK
            var itemEx = SetDataForDocument(item);
            var itemSchema = itemEx as IDictionary<string, object>;
            var sbParamGroup = new StringBuilder();
            if (autoPkColumn != null && itemSchema.ContainsKey(autoPkColumn.PropertyName)) {
              itemSchema.Remove(autoPkColumn.PropertyName);
            }
            if (ReferenceEquals(item, first)) {
              var sbFieldNames = new StringBuilder();
              foreach (var field in itemSchema) {
                string mappedColumnName = this.TableMapping.ColumnMappings.FindByProperty(field.Key).DelimitedColumnName;
                sbFieldNames.AppendFormat("{0},", mappedColumnName);
              }
              var delimitedColumnNames = sbFieldNames.ToString().Substring(0, sbFieldNames.Length - 1);
              string stub = "INSERT INTO {0} ({1}) VALUES ";
              insertClause = string.Format(stub, this.TableMapping.DelimitedTableName, string.Join(", ", delimitedColumnNames));
              sbSql = new StringBuilder(insertClause);
            }
            foreach (var key in itemSchema.Keys) {
              if (paramCounter + itemSchema.Count >= MAGIC_PG_PARAMETER_LIMIT || rowValueCounter >= MAGIC_PG_ROW_VALUE_LIMIT) {
                dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
                commands.Add(dbCommand);
                sbSql = new StringBuilder(insertClause);
                paramCounter = 0;
                rowValueCounter = 0;
                dbCommand = this.Model.CreateCommand("", connection);
                dbCommand.Transaction = tdbTransaction;
              }
              if (key == "search") {
                sbParamGroup.AppendFormat("to_tsvector(@{0}),", paramCounter.ToString());
              } else {
                sbParamGroup.AppendFormat("@{0},", paramCounter.ToString());
              }
              dbCommand.AddParam(itemSchema[key]);
              paramCounter++;
            }
            // Add the row params to the end of the sql:
            sbSql.AppendFormat("({0}),", sbParamGroup.ToString().Substring(0, sbParamGroup.Length - 1));
            rowValueCounter++;
          }
          dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
          commands.Add(dbCommand);
          try {
            foreach (var cmd in commands) {
              rowsAffected += cmd.ExecuteNonQuery();
            }
            tdbTransaction.Commit();
          }
          catch (Exception) {
            tdbTransaction.Rollback();
          }
        }
      }
      return items;
    }

    /// <summary>
    /// Updates a single T item
    /// </summary>
    public override T Update(T item) {
      var expando = SetDataForDocument(item);
      this.Model.Update(expando);
      return item;
    }

    public override T Delete(T item) {
      Model.Delete(item);
      return item;
    }

    public override List<T> Delete(List<T> items) {
      // NOTE: We need to implement this directly in here because the Model is typed 
      // dynammically, so a call into the model defaults to the Delete(item) method, 
      // and fails to parse the list object. Calling into the Model.Delete(List<T> items)
      // Method throws because on the model, T is type dynamic. List<T> cannot be cast to List<dynamic>.

      var removed = 0;
      if (items.Count() > 0) {
        string keyColumnNames;
        var keyList = new List<string>();

        // If a compund key is present, we need to handle things a little differently:
        if (this.TableMapping.HasCompoundPk) {
          // we need to build a DELETE with an IN statement like this:
          // DELETE FROM myTable WHERE (pk1, pk2) IN ((value1, value2), (value3, value4), ...)
          var columnArray = from k in this.TableMapping.PrimaryKeyMapping select k.DelimitedColumnName;
          keyColumnNames = string.Format("({0})", string.Join(",", columnArray));
          foreach (var item in items) {
            var expando = item.ToExpando();
            var dict = (IDictionary<string, object>)expando;
            var sbValues = new StringBuilder("");
            foreach (var pk in this.TableMapping.PrimaryKeyMapping) {
              sbValues.Append(string.Format("{0},", dict[pk.PropertyName].ToString()));
            }
            string values = sbValues.ToString().Substring(0, sbValues.Length - 1);
            keyList.Add(string.Format("({0})", values));
          }
        } else {
          // Otherwise, the first pk in the list is what we want:
          keyColumnNames = this.TableMapping.PrimaryKeyMapping[0].DelimitedColumnName;
          foreach (var item in items) {
            var expando = item.ToExpando();
            var dict = (IDictionary<string, object>)expando;
            keyList.Add(dict[this.TableMapping.PrimaryKeyMapping[0].PropertyName].ToString());
          }
        }
        var keySet = String.Join(",", keyList.ToArray());
        var inStatement = keyColumnNames + " IN (" + keySet + ")";
        removed = Model.DeleteWhere(inStatement);
      }
      return items;
    }
  }
}
