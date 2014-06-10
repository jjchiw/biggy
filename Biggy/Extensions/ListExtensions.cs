using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Extensions
{
    public static class ListExtensions
    {
        public static void Load<T>(this LazyLoadingCollection<T> list, BiggyRelationalStore<dynamic> store, string property, int skip, int take, object parent) where T : new()
        {
            var where = store.BuildWherePrimarykey(parent);
            string sql = store.BuildSelect(where, "", take, skip);
            var query = string.Format(sql, "json_array_elements(" + property + ") as " + property, store.TableMapping.DelimitedTableName);
            var results = store.Query(query);

            var sb = new StringBuilder();
            foreach (var item in results)
            {
                var dict = (item as object).ToDictionary();
                sb.AppendFormat("{0},", dict[property]);
            }
            // Can't take a substring of a zero-length string:
            if (sb.Length > 0)
            {
                var scrunched = sb.ToString();
                var stripped = scrunched.Substring(0, scrunched.Length - 1);
                var json = string.Format("[{0}]", stripped);
                list.Add(JsonConvert.DeserializeObject<List<T>>(json));
            }
        }
    }
}
