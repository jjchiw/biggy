using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;

namespace Biggy.SqlCe {
  public class SqlCeDocumentStore<T> : SQLServer.SQLDocumentStore<T> where T : new() {
    
    public SqlCeDocumentStore(DbCache dbCache) : base(dbCache, null) { }
    public SqlCeDocumentStore(DbCache dbCache, string tableName) : base(dbCache, tableName) { }
    public SqlCeDocumentStore(string connectionStringName) : base(new SqlCeCache(connectionStringName)) { }
    public SqlCeDocumentStore(string connectionStringName, string tableName) : base(new SqlCeCache(connectionStringName), tableName) { }


    public override BiggyRelationalStore<dynamic> getModel() {
      return new SqlCeStore<dynamic>(this.DbCache);
    }

    public override T Insert(T item) {
      var expando = SetDataForDocument(item);
      expando = Model.Insert(expando);
      var pkMap = Model.TableMapping.PrimaryKeyMapping.Single();       //TODO: compound Pk not supported
      if (pkMap.IsAutoIncementing) {
        var newId = Model.GetPropertyValue(expando, pkMap.PropertyName);
        this.Model.SetPropertyValue(item, pkMap.PropertyName, newId);
        // update document body of autoinc Pk value (insert and update should go within transaction, wait for Biggy transactions)
        Update(item);
      }
      return item;
    }

    public override List<T> BulkInsert(List<T> items) {
      if (false == items.Any()) return items;
      var first = items.First();
      var expando = SetDataForDocument(first);

      var pkMap = Model.TableMapping.PrimaryKeyMapping.Single();  //TODO: compound Pk not supported
      var insertCmd = Model.CreateInsertCommand(expando, null);
      var updateSql = string.Format("update {0} set [body] = @0 where {1} = @1",
                  Model.TableMapping.DelimitedTableName, pkMap.DelimitedColumnName);

      bool fetchNewId = pkMap.IsAutoIncementing;

      // Reuse a connection and commands object, SqlCe has a limit of open sessions
      using (var conn = Model.Cache.OpenConnection())
      using (var tx = conn.BeginTransaction()) {
        // prepare commands
          var newIdQuery = conn.CreateCommand("select @@Identity");
        // we'll need update command only if Pk is auto-inc, so we'd need to update doc's body
        var updateCmd = conn.CreateCommand(updateSql, "body-stub", 0);

        insertCmd.Connection = conn;
        newIdQuery.Transaction = insertCmd.Transaction = updateCmd.Transaction = tx;

        foreach (var item in items) {
          if (false == object.ReferenceEquals(first, item)) {
            expando = SetDataForDocument(item);
            var args = expando.GetInsertParamValues(pkMap);
            insertCmd.SetNewParameterValues(args);
          }
          insertCmd.ExecuteNonQuery();

          if (true == fetchNewId) {
            var newId = newIdQuery.ExecuteScalar();
            Model.SetPropertyValue(item, pkMap.PropertyName, newId);
            expando = SetDataForDocument(item);
            updateCmd.Parameters[0].Value = ((dynamic)expando).body as string;
            updateCmd.Parameters[1].Value = newId;
            updateCmd.ExecuteNonQuery();
          }
        }
        tx.Commit();
      }
      return items;
    }

    protected override List<T> TryLoadData() {
      try {
        return this.LoadAll();
      }
      catch (System.Data.SqlServerCe.SqlCeException x) {
        if (x.Message.StartsWith("The specified table does not exist.")) {
          var pkMap = TableMapping.PrimaryKeyMapping.Single();    //TODO: compound Pk not supported
          //create the table
          var idType = pkMap.IsAutoIncementing ? " int identity(1,1)" : " nvarchar(255)";
          string fullTextColumn = string.Empty;
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search ntext";
          }
          var sql = string.Format(
              "CREATE TABLE {0} ({1} {2} primary key not null, body ntext not null{3});",
              this.TableMapping.DelimitedTableName, pkMap.DelimitedColumnName, idType, fullTextColumn);
          this.Model.Execute(sql);

          return TryLoadData();
        } else { throw; }
      }
    }
  }
}
