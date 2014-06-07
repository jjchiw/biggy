using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Npgsql;
using System.Configuration;

namespace Biggy.Postgres
{
  public class PGCache : DbCache
  {
    public PGCache(string connectionStringName) : base(connectionStringName) { }

    public override string DbDelimiterFormatString {
      get { return "\"{0}\""; }
    }

    public override DbConnection OpenConnection() {
      var result = new NpgsqlConnection(this.ConnectionString);
      result.Open();
      return result;
    }

    protected override void LoadDbColumnsList() {
      this.DbColumnsList = new List<DbColumnMapping>();
      var sql = ""
        + "SELECT c.TABLE_NAME, c.COLUMN_NAME, kcu.CONSTRAINT_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, tc.CONSTRAINT_TYPE, "
          + "CASE tc.CONSTRAINT_TYPE WHEN 'PRIMARY KEY' THEN CAST(1 AS BIt) ELSE CAST(0 AS Bit) END AS IsPrimaryKey, "
          + "(CASE ((SELECT CASE (LENGTH(pg_get_serial_sequence(quote_ident(c.TABLE_NAME), c.COLUMN_NAME)) > 0) WHEN true THEN 1 ELSE 0 END) + "
          + "(SELECT CASE (SELECT pgc.relkind FROM pg_class pgc WHERE pgc.relname = c.TABLE_NAME || '_' || c.COLUMN_NAME || '_' || 'seq') WHEN 'S' THEN 1 ELSE 0 END)) "
          + "WHEN 0 THEN false ELSE true END) AS IsAuto "
          + "FROM information_schema.columns c "
          + "LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu "
        + "ON c.TABLE_SCHEMA = kcu.CONSTRAINT_SCHEMA AND c.TABLE_NAME = kcu.TABLE_NAME AND c.COLUMN_NAME = kcu.COLUMN_NAME "
        + "LEFT OUTER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc "
        + "ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME "
        + "WHERE c.TABLE_SCHEMA = 'public'";

      using (var conn = this.OpenConnection()) {
        using (var cmd = conn.CreateCommand()) {
          cmd.CommandText = sql;
          var dr = cmd.ExecuteReader();
          while (dr.Read()) {
            var clm = dr["COLUMN_NAME"] as string;
            var newColumnMapping = new DbColumnMapping(this.DbDelimiterFormatString) {
              TableName = dr["TABLE_NAME"] as string,
              ColumnName = clm,
              PropertyName = clm,
              IsPrimaryKey = (bool)dr["IsPrimaryKey"],
              IsAutoIncementing = (bool)dr["IsAuto"]
            };
            this.DbColumnsList.Add(newColumnMapping);
          }
        }
      }
    }

    protected override void LoadDbTableNames() {
      this.DbTableNames = new List<string>();
      var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'public'";
      using (var conn = this.OpenConnection()) {
        using (var cmd = conn.CreateCommand()) {
          cmd.CommandText = sql;
          var dr = cmd.ExecuteReader();
          while (dr.Read()) {
            this.DbTableNames.Add(dr.GetString(0));
          }
        }
      }
    }

    // TODO: Somehow handle the inconsistent useage of delimited/not delimited table name:
    public override bool TableExists(string notDelimitedTableName) {
      bool exists = false;
      string select = ""
          + "SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES "
          + "WHERE TABLE_SCHEMA = 'public' "
          + "AND  TABLE_NAME = '{0}'";
      string sql = string.Format(select, notDelimitedTableName);
      using (var conn = this.OpenConnection()) {
        using (var cmd = conn.CreateCommand()) {
          cmd.CommandText = sql;
          var result = Convert.ToInt32(cmd.ExecuteScalar());
          if (result > 0) {
            exists = true;
          }
        }
      }
      return exists;
    }

    public string ToTableName(string typeName) {
      var sb = new StringBuilder(typeName);
      int insertions = 0;
      sb[0] = char.ToLower(sb[0]);
      for (int i=1; i<typeName.Length; ++i) {
        if (char.IsUpper(typeName[i])) {
    	  sb[i+insertions] = char.ToLower(typeName[i]);
    	  sb.Insert(i+insertions, '_');
    	  insertions++;
    	}
      }
      return sb.ToString();
    }
 
    public string ToClassName(string tableName) {
      var sb = new StringBuilder(tableName);
      int deletions = 0;
      sb[0] = char.ToUpper(sb[0]);
      for (int i=1; i<tableName.Length; ++i) {
        if (tableName[i] == '_') {
    	  sb.Remove(i-deletions, 1);
    	  sb[i-deletions] = char.ToUpper(sb[i-deletions]);
    	  deletions++;
    	}
      }
      return sb.ToString();
    }

    public List<string> GetColumnsNames(string tableName) {
        var columns = new List<string>();
        string select = @"
                        SELECT column_name
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = 'public'
                        AND  TABLE_NAME = '{0}';";
        string sql = string.Format(select, tableName);
        using (var conn = this.OpenConnection())
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    columns.Add(dr["column_name"] as string);
                }
            }
        }

        return columns;
    }
  }
}
