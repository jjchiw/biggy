using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using Npgsql;
using Biggy.Extensions;

namespace Biggy.Postgres
{
  public class PGStore<T> : BiggyRelationalStore<T> where T : new() {
    public PGStore(DbCache dbCache) : base(dbCache) { }
    public PGStore(string connectionString) : base(new PGCache(connectionString)) { }

    public override DbConnection OpenConnection() {
      var result = new NpgsqlConnection(this.ConnectionString);
      result.Open();
      return result;
    }

    public override string GetInsertReturnValueSQL(string delimitedPkColumn) {
      return " RETURNING " + delimitedPkColumn + " as newId";
    }

    public override string GetSingleSelect(string delimitedTableName, string where) {
      return string.Format("SELECT * FROM {0} WHERE {1} LIMIT 1", delimitedTableName, where);
    }

    public override string BuildSelect(string where, string orderBy, int limit)
    {
        return BuildSelect(where, orderBy, limit, 0);
    }

    public override string BuildSelect(string where, string orderBy, int limit, int offset) {
      string sql = "SELECT {0} FROM {1} ";
      if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;
      }
      if (!String.IsNullOrEmpty(orderBy)) {
        sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;
      }

      if (limit > 0) {
        sql += " LIMIT " + limit;
      }

      if (offset > 0)
      {
          sql += " OFFSET " + offset;
      }
      return sql;
    }
  }
}
