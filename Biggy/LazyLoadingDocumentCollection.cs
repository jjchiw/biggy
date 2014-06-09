using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;

namespace Biggy
{
    public class LazyLoadingDocumentCollection<T> : ILazyLoadingCollection<T> where T : new()
    {
        List<T> _list;
        public LazyLoadingDocumentCollection()
        {
            _list = new List<T>();
            //_store = store;
        }

        public void Remove(T item)
        {
            _list.Remove(item);
        }

        public void Remove(List<T> items)
        {
            foreach (var item in items)
            {
                Remove(item);
            }
        }

        public void Add(T item)
        {
            _list.Add(item);
            //agregar en la columna
        }

        public void Add(List<T> items)
        {
            _list.AddRange(items);
            //agregar en la columna
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Load(BiggyRelationalStore<dynamic> store, string property, int skip, int take, object parent, params object[] args)
        {
            var where = store.BuildWherePrimarykey(parent);
            string sql = store.BuildSelect(where, "", take, skip);
            var query = string.Format(sql, "json_array_elements("+property+") as " + property, store.TableMapping.DelimitedTableName);
            var results = store.Query(query, args);

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
                _list.AddRange(JsonConvert.DeserializeObject<List<T>>(json));
            }
        }
    }
}
