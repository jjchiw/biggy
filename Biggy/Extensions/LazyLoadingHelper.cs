using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;

namespace Biggy.Extensions
{
    class LazyLoadingHelper
    {
        public static string Update(BiggyRelationalStore<dynamic> store, 
                                      ILazyLoadingCollection collection, 
                                      Type collectionType,
                                      string property,
                                      string primarykeyName,
                                      object parent)
        {
            var where = store.BuildWherePrimarykey(parent);

            //var azzz = ObjectExtensions.ToDictionary(where);

            var removedCollection = collection.Removed.Select(x => ObjectExtensions.ToDictionary(x) as IDictionary<string, object>).ToList();
            var addedCollection = collection.Added.Select(x => ObjectExtensions.ToDictionary(x) as IDictionary<string, object>).ToList();

            string sql = store.BuildSelect(where, "", 0, 0);
            var query = string.Format(sql, "json_array_elements(" + property + ") as " + property, store.TableMapping.DelimitedTableName);
            var queryResults = store.Query(query).Select(x =>
            {
                return JsonConvert.DeserializeObject((x as IDictionary<string, object>)[property] as string, collectionType).ToDictionary();
            }).ToList();
                

            var updatedListAfterDelete = queryResults.Where(x => !removedCollection.Any(y => y[primarykeyName].Equals(x[primarykeyName]))).ToList();
            var updatedAddedWithDelete = addedCollection.Where(x => !removedCollection.Any(y => y[primarykeyName].Equals(x[primarykeyName]))).ToList();

            var yaya = updatedListAfterDelete.Select(x => x[primarykeyName]).ToList();
            var yeye = updatedAddedWithDelete.Select(x => x[primarykeyName]).ToList();


            var similars = updatedListAfterDelete.Where(x => updatedAddedWithDelete.Any(y => y[primarykeyName].Equals(x[primarykeyName]))).ToList();
            var different1 = updatedListAfterDelete.Where(x => !updatedAddedWithDelete.Any(y => y[primarykeyName].Equals(x[primarykeyName]))).ToList();
            var different2 = updatedAddedWithDelete.Where(x => !updatedListAfterDelete.Any(y => y[primarykeyName].Equals(x[primarykeyName]))).ToList();

            var result = similars.Concat(different1).Concat(different2).ToList();

            return JsonConvert.SerializeObject(result);
            
        }
    }
}
