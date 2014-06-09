using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data.SqlClient;
using Biggy.Extensions;

namespace Biggy.SQLServer {
  public class SQLServerStore<T> : BiggyRelationalStore<T> where T : new() {
    public SQLServerStore(DbCache dbCache) : base(dbCache) { }
    public SQLServerStore(string connectionString) : base(new SQLServerCache(connectionString)) { }

    public override DbConnection OpenConnection() {
      var conn = new SqlConnection(this.ConnectionString);
      conn.Open();
      return conn;
    }

    public override string GetInsertReturnValueSQL(string delimitedPkColumn) {
      return string.Format("; SELECT SCOPE_IDENTITY() as {0}", delimitedPkColumn);
    }

    public override string BuildSelect(string where, string orderBy, int limit) {
      string sql = limit > 0 ? "SELECT TOP " + limit + " {0} FROM {1} " : "SELECT {0} FROM {1} ";
      if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;
      }
      if (!String.IsNullOrEmpty(orderBy)) {
        sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;
      }
      return sql;
    }

    public override string BuildSelect(string where, string orderBy = "", int limit = 0, int offset = 0)
    {
        throw new NotImplementedException();
    }

    public override string GetSingleSelect(string delimitedTableName, string where) {
      return string.Format("SELECT TOP 2 * FROM {0} WHERE {1}", delimitedTableName, where);
    }

    public override int Delete(List<T> items) {
      // The boys at MS can't seem to get full support for Tuples into the Where . . . IN
      // Statement for SQL Server, so we have to do an ugly override, in case someone uses 
      // Composite Primary Keys . . .
      // In Postgres, Oracle, and even MySql we can build THIS:
      // DELETE FROM myTable WHERE (pk1, pk2, ...) IN ((value1, value2), (value3, value4), ...)
      // But SQL Server does not support this syntax. Hence, the following:

      var removed = 0;
      if (items.Count() > 0) {
        var keyList = new List<string>();
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
          removed = this.DeleteWhere(criteriaStatement);
        } else {
          // There's just a single PK. Use the base method:
          removed = base.Delete(items);
        }
      }
      return removed;
    }
  }
}
