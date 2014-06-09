using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using Biggy.Extensions;

namespace Biggy.SQLServer {
  public class SQLDocumentStore<T> : BiggyDocumentStore<T> where T : new() {

    public SQLDocumentStore(DbCache dbCache) : base(dbCache) { }
    public SQLDocumentStore(DbCache dbCache, string tableName) : base(dbCache, tableName) { }
    public SQLDocumentStore(string connectionStringName) : base(new SQLServerCache(connectionStringName)) { }
    public SQLDocumentStore(string connectionStringName, string tableName) : base(new SQLServerCache(connectionStringName), tableName) { }

    public override BiggyRelationalStore<dynamic> getModel() {
      return new SQLServerStore<dynamic>(this.DbCache);
    }

    protected override List<T> TryLoadData() {
      try {
        return this.LoadAll();
      }
      catch (System.Data.SqlClient.SqlException x) {
        if (x.Message.Contains("Invalid object name")) {         
          //create the table
          var sql = "";
          var keyColumnDefs = new List<string>();
          var keyColumnNames = new List<string>();

          string keyDefStub = "{0} {1} not null";
          foreach (var pk in Model.TableMapping.PrimaryKeyMapping) {
            string integerKeyType = "int";
            if (pk.IsAutoIncementing) {
              integerKeyType = "int identity(1,1)";
            }
            var pkType = pk.DataType == typeof(int) ? " " + integerKeyType : "nvarchar(255)";
            keyColumnDefs.Add(string.Format(keyDefStub, pk.DelimitedColumnName, pkType));
            keyColumnNames.Add(pk.DelimitedColumnName);
          }
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search nvarchar(MAX)";
          }
          string keyColumnsStatement = string.Join(",", keyColumnDefs.ToArray());
          string pkColumns = string.Join(",", keyColumnNames.ToArray());
          sql = string.Format("CREATE TABLE {0} ({1}, body nvarchar(MAX) not null {2}, PRIMARY KEY ({3}));", this.TableMapping.DelimitedTableName, keyColumnsStatement, fullTextColumn, pkColumns);
          
          this.Model.Execute(sql);
          //if (this.FullTextFields.Length > 0) {
          //  var indexSQL = string.Format("CREATE FULL TEXT INDEX ON {0}({1})",this.TableName,string.Join(",",this.FullTextFields));
          //  this.Model.Execute(indexSQL);
          //}
          return this.TryLoadData();
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
      var expando = SetDataForDocument(item);
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
        vals.Add(string.Format("@{0}", index));
        args.Add(dc[key]);
        columnNames.Add(this.TableMapping.ColumnMappings.FindByProperty(key).DelimitedColumnName);
        index++;
      }
      var sb = new StringBuilder();
      sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}); SELECT SCOPE_IDENTITY() as newID;", this.TableMapping.DelimitedTableName, string.Join(",", columnNames), string.Join(",", vals));
      var sql = sb.ToString();
      var newKey = this.Model.Scalar(sql, args.ToArray());
      //set the key
      if (autoPkColumn != null) {
        this.Model.SetPropertyValue(item, autoPkColumn.PropertyName, newKey);
      }
    }

    /// <summary>
    /// A high-performance bulk-insert that can drop 10,000 documents in about 500ms
    /// </summary>
    public override List<T> BulkInsert(List<T> items) {
      // These are SQL Server Max values:
      const int MAGIC_SQL_PARAMETER_LIMIT = 2100;
      const int MAGIC_SQL_ROW_VALUE_LIMIT = 1000;
      int rowsAffected = 0;

      var first = items.First();

      string insertClause = "";
      var sbSql = new StringBuilder("");

      using (var connection = this.DbCache.OpenConnection()) {
        using (var tdbTransaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)) {
          var commands = new List<DbCommand>();
          // Lock the table, so nothing will disrupt the pk sequence:
          string lockTableSQL = string.Format("SELECT 1 FROM {0} WITH(TABLOCKX) ", this.TableMapping.DelimitedTableName);
          DbCommand dbCommand = this.Model.CreateCommand(lockTableSQL, connection);
          dbCommand.Transaction = tdbTransaction;
          dbCommand.ExecuteNonQuery();
          var autoPkColumn = this.TableMapping.PrimaryKeyMapping.FirstOrDefault(c => c.IsAutoIncementing == true);

          int nextSerialPk = 0;
          if (autoPkColumn != null) {
            // Now get the next Identity Id. ** Need to do this within the transaction/table lock scope **:
            // NOTE: The application must have ownership permission on the table to do this!!
            var sql_get_seq = string.Format("SELECT IDENT_CURRENT ('{0}' )", this.TableMapping.DelimitedTableName);
            dbCommand.CommandText = sql_get_seq;
            // if this is a fresh sequence, the "seed" value is returned. We will assume 1:
            nextSerialPk = Convert.ToInt32(dbCommand.ExecuteScalar());
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
              if (paramCounter + itemSchema.Count >= MAGIC_SQL_PARAMETER_LIMIT || rowValueCounter >= MAGIC_SQL_ROW_VALUE_LIMIT) {
                dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
                commands.Add(dbCommand);
                sbSql = new StringBuilder(insertClause);
                paramCounter = 0;
                rowValueCounter = 0;
                dbCommand = this.Model.CreateCommand("", connection);
                dbCommand.Transaction = tdbTransaction;
              }
              // FT SEARCH STUFF SHOULD GO HERE
              sbParamGroup.AppendFormat("@{0},", paramCounter.ToString());
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

      // NOTE II: The boys at MS can't seem to get full support for Tuples into the Where . . . IN
      // Statement for SQL Server, so we have to do an ugly override, in case someone uses 
      // Composite Primary Keys . . .
      // In Postgres, Oracle, and even MySql we can build THIS:
      // DELETE FROM myTable WHERE (pk1, pk2, ...) IN ((value1, value2), (value3, value4), ...)
      // But SQL Server does not support this syntax. Hence, the following:

      var removed = 0;
      if (items.Count() > 0) {
        string criteriaStatement = "";
        // If a composite key is present, we need to handle things a little differently:
        if (this.TableMapping.HasCompoundPk) {
          // We need to build a DELETE with an standard crtieria statement like this:
          // DELETE FROM myTable WHERE (pk1 = value1 AND pk2 = value2) OR (pk1 = value3 AND pk2 = value4) OR ...etc
          var andList = new List<string>();
          foreach (var item in items) {
            var expando = item.ToExpando();
            var dict = (IDictionary<string, object>)expando;
            var conditionsList = new List<string>();
            foreach (var pk in this.TableMapping.PrimaryKeyMapping) {
              // Most often it will be an int:
              string conditionFormatString = "{0} = {1}";
              if (pk.DataType == typeof(string)) {
                // Wrap in single quotes
                conditionFormatString = "{0} = '{1}'";
              }
              string condition = string.Format(conditionFormatString, pk.DelimitedColumnName, dict[pk.PropertyName]);
              conditionsList.Add(condition);
            }
            andList.Add(string.Format("({0})", string.Join(" AND ", conditionsList.ToArray())));
          }
          criteriaStatement = string.Join(" OR ", andList.ToArray());
          removed = Model.DeleteWhere(criteriaStatement);
        } else {
          // There's just a single PK. Use the base method:
          // Otherwise, the first pk in the list is what we want:
          var keyList = new List<string>();
          string keyColumnNames = this.TableMapping.PrimaryKeyMapping[0].DelimitedColumnName;
          foreach (var item in items) {
            var expando = item.ToExpando();
            var dict = (IDictionary<string, object>)expando;
            var pk = this.TableMapping.PrimaryKeyMapping[0];
            if (pk.DataType == typeof(string)) {
              // Wrap in single quotes
              keyList.Add(string.Format("'{0}'", dict[pk.PropertyName].ToString()));
            } else {
              // Don't wrap:
              keyList.Add(dict[pk.PropertyName].ToString());
            }
          }
          var keySet = String.Join(",", keyList.ToArray());
          criteriaStatement = keyColumnNames + " IN (" + keySet + ")";
          removed = Model.DeleteWhere(criteriaStatement);
        }
      }
      return items;
    }
  }
}
