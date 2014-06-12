using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;

namespace Biggy.SqlCe {
  public class SqlCeStore<T> : Biggy.SQLServer.SQLServerStore<T> where T : new() {
    public override DBTableMapping getTableMappingForT() {
      var maps = base.getTableMappingForT();
      // just for now, remove this method when compound Pks will be ready
      if (maps.HasCompoundPk)
        throw new NotImplementedException("Sorry guys, no compound Pk supported for SqlCe.");
      
      return maps;
    }

    public SqlCeStore(DbCache dbCache) : base(dbCache) { }
    public SqlCeStore(string connectionString) : base(new SqlCeCache(connectionString)) { }

    public override DbConnection OpenConnection() {
      return this.Cache.OpenConnection();
    }

    public override T Insert(T item) {
      if (BeforeSave(item)) {
        object newId = null;
        var cmd = (DbCommand)this.CreateInsertCommand(item, null);

        using (var conn = OpenConnection())
        using (var tx = conn.BeginTransaction()) {
          cmd.Transaction = tx;
          int rowsCnt = cmd.ExecuteNonQuery();

          // retrieve new Id
          cmd.CommandText = "select @@Identity";
          newId = cmd.ExecuteScalar();

          if (rowsCnt == 1 && newId != null)
            tx.Commit();
          //TODO: ?? else return null;
        }
        var pk = TableMapping.PrimaryKeyMapping.Single();   //TODO: compound Pk not supported
        if (pk.IsAutoIncementing)
            this.SetPropertyValue(item, pk.PropertyName, newId);
        Inserted(item);
      }
      return item;
    }

    // It can be done much better, see: http://sqlcebulkcopy.codeplex.com/, but this shouldn't be very bad.
    // I also tried to make one generic BulkInserter for store and documentStore but there are many generic/dynamic things so maybe not worth it 
    public override List<T> BulkInsert(List<T> items) {
      if (false == items.Any()) {
        return items;
      }
      DBTableMapping dbtmap = this.TableMapping;
      var pkMap = dbtmap.PrimaryKeyMapping.First();//HACK: Now everywhere is assumed there is single column Pk

      var first = items.First();
      var insertCmd = this.CreateInsertCommand(first, null);

      // Reuse a connection and commands object, SqlCe has a limit of open sessions
      using (var conn = OpenConnection())
      using (var tx = conn.BeginTransaction()) {
        insertCmd.Transaction = tx;

        DbCommand newIdQuery = null;
        if (pkMap.IsAutoIncementing) {
          newIdQuery = conn.CreateCommand("select @@Identity");
          newIdQuery.Transaction = tx;
        }

        foreach (var item in items) {
          if (false == object.ReferenceEquals(first, item)) {
            var pValues = item.GetInsertParamValues(pkMap);
            System.Diagnostics.Debug.Assert(insertCmd.Parameters.Count == pValues.Count());
            insertCmd.SetNewParameterValues(pValues);
          }
          insertCmd.ExecuteNonQuery();

          if (pkMap.IsAutoIncementing) {
            var newId = newIdQuery.ExecuteScalar();
            this.SetPropertyValue(item, pkMap.PropertyName, newId);
          }
        }
        tx.Commit();
      }
      return items;
    }
  }
}
