using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Extensions
{
    public static class DbConnectionExtensions
    {
        public static DbCommand CreateCommand(this DbConnection conn, string sql, params object[] args)
        {
            var result = (DbCommand)conn.CreateCommand();
            result.CommandText = sql;
            if (args.Length > 0)
            {
                result.AddParams(args);
            }
            return result;
        }
    }
}
