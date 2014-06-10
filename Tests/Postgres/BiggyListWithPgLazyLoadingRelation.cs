using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Biggy.Postgres;
using Xunit;
using Newtonsoft.Json;
using Biggy.Extensions;

namespace Tests.Postgres
{
    public class StoreDb
    {
        readonly Dictionary<Type, object> _stores;
        readonly Dictionary<Type, object> _documents;
        readonly string _connectionStringName;
        readonly bool _inMemoryList;

        public StoreDb(string connectionStringName) : this(connectionStringName, false) { }

        public StoreDb(string connectionStringName, bool inMemoryList)
        {
            _connectionStringName = connectionStringName;
            _stores = new Dictionary<Type, object>();
            _documents = new Dictionary<Type, object>();
            _inMemoryList = inMemoryList;
        }

        public IBiggyStore<T> GetStoreDb<T>() where T : new()
        {
            IBiggyStore<T> result;
            object resultTemp;

            if (_stores.TryGetValue(typeof(T), out resultTemp))
            {
                return resultTemp as IBiggyStore<T>;
            }

            result = new PGDocumentStore<T>(_connectionStringName);
            _stores.Add(typeof(T), result);
            return result;
        }


        public IBiggy<T> GetBiggyList<T>() where T : new()
        {
            IBiggy<T> result;
            object resultTemp;

            if (_documents.TryGetValue(typeof(T), out resultTemp))
            {
                return resultTemp as IBiggy<T>;
            }

            result = new BiggyList<T>(GetStoreDb<T>(), _inMemoryList);
            _documents.Add(typeof(T), result);
            return result;
        }
    }


    [Trait("Biggy List With Postgres Document Store, Lazy Loading Relations", "")]
    public class BiggyListWithPgLazyLoadingRelation
    {
        string _connectionStringName = "clownsPG";
        StoreDb _store;
        PGCache _cache;

        public BiggyListWithPgLazyLoadingRelation()
        {
            _store = new StoreDb(_connectionStringName);

            _cache = new PGCache(_connectionStringName);
            // This one will be re-created automagically:
            if (_cache.TableExists("ClownDocuments"))
            {
                _cache.DropTable("ClownDocuments");
            }

            if (_cache.TableExists("PartyDocuments"))
            {
                _cache.DropTable("PartyDocuments");
            }
        }


        [Fact(DisplayName = "Creates a store with a json column related to the collection that will be lazy loaded")]
        public void Creates_Document_Table_With_Json_Column_Collection()
        {
            var columns = _cache.GetColumnsNames("ClownDocuments");

            Assert.Equal(5, columns.Count());

        }

        [Fact(DisplayName = "Adds a document with a relations")]
        public void Adds_Document_With_Relations()
        {
            var newClown = CreateClown();
            _store.GetBiggyList<ClownDocument>().Add(newClown);

            Assert.Equal(1, _store.GetBiggyList<ClownDocument>().Count());
        }

        [Fact(DisplayName = "Adds a document with a relations and load lazy loaded")]
        public void Adds_Document_With_Relations_And_Load_Lazy()
        {
            var newClown = CreateClown();
            _store.GetBiggyList<ClownDocument>().Add(newClown);

            var store = new PGDocumentStore<ClownDocument>(_connectionStringName);

            var newClownDocumentLL = new BiggyList<ClownDocument>(store);

            var clown = newClownDocumentLL.First();

            Assert.Equal(0, clown.Parties.Count());
            Assert.Equal(0, clown.Schedules.Count());
            Assert.Equal(3, clown.OtherNames.Count());

            clown.Parties.Load<PartyDenormalized<PartyDocument>>(store.Model, "parties", 0, 2, clown);
            
            Assert.Equal(2, clown.Parties.Count());

            clown.Schedules.Load<Schedule>(store.Model, "schedules", 2, 1, clown);

            Assert.Equal(1, clown.Schedules.Count());
            Assert.Equal("New Wig", clown.Schedules.FirstOrDefault().Name);
        }

        [Fact(DisplayName = "Update a document with a new relations marked as LazyLoading")]
        public void Update_Document_With_Relations_And_Load_Lazy()
        {
            var newClown = CreateClown();
            _store.GetBiggyList<ClownDocument>().Add(newClown);

            var store = new PGDocumentStore<ClownDocument>(_connectionStringName);

            var newClownDocumentLL = new BiggyList<ClownDocument>(store);

            var clown = newClownDocumentLL.First();

            Assert.Equal(0, clown.Parties.Count());
            Assert.Equal(0, clown.Schedules.Count());
            Assert.Equal(3, clown.OtherNames.Count);

            clown.Parties.Load<PartyDenormalized<PartyDocument>>(store.Model, "parties", 0, 2, clown);

            Assert.Equal(2, clown.Parties.Count());

            clown.Schedules.Load<Schedule>(store.Model, "schedules", 2, 1, clown);

            Assert.Equal(1, clown.Schedules.Count());
            Assert.Equal("New Wig", clown.Schedules.FirstOrDefault().Name);

            clown.Schedules.Add(
                new Schedule 
                    {
                        Name = "Need new horn for my bicycle",
                        BeginDate = DateTime.Now.AddDays(4),
                        EndDate = DateTime.Now.AddDays(4).AddHours(1)
                    }
            );

            _store.GetBiggyList<ClownDocument>().Update(clown);
        }

        private ClownDocument CreateClown()
        {
            var party = new PartyDocument
            {
                Name = "Andrew Big Party",
                Address = "742 Evergreen Terrace",
                Date = DateTime.Now.AddDays(100)
            };

            var party2 = new PartyDocument
            {
                Name = "Samantha Big Party",
                Address = "1 Virginia Street, London E1",
                Date = DateTime.Now.AddDays(100)
            };

            _store.GetBiggyList<PartyDocument>().Add(party);
            _store.GetBiggyList<PartyDocument>().Add(party2);

            var newClown = new ClownDocument
            {
                LifeStory = "English actor, comedian and dancer, who became the most popular English entertainer of the Regency era",
                Name = "Joseph Grimaldi",
                Schedules = new LazyLoadingCollection<Schedule>
                {
                    new Schedule 
                    {
                        Name = "Wash my red nose",
                        BeginDate = DateTime.Now.AddDays(1),
                        EndDate = DateTime.Now.AddDays(1).AddHours(1)
                    },
                    new Schedule 
                    {
                        Name = "Buy some big shoes",
                        BeginDate = DateTime.Now.AddDays(2),
                        EndDate = DateTime.Now.AddDays(2).AddHours(1)
                    },
                    new Schedule 
                    {
                        Name = "New Wig",
                        BeginDate = DateTime.Now.AddDays(3),
                        EndDate = DateTime.Now.AddDays(3).AddHours(1)
                    }
                },
                Parties = new LazyLoadingCollection<PartyDenormalized<PartyDocument>> { party, party2 },
                OtherNames = new List<string> { "Yakko", "Wakko", "Dot"}
            };

            return newClown;
        }


        //[Fact(DisplayName = "Updates a document with a serial PK")]
        //public void Updates_Document_With_Serial_PK() {
        //  var newCustomer = new ClientDocument {
        //    Email = "rob@tekpub.com",
        //    FirstName = "Rob",
        //    LastName = "Conery"
        //  };
        //  _clownDocuments.Add(newCustomer);
        //  int idToFind = newCustomer.ClientDocumentId;

        //  // Go find the new record after reloading:
        //  _clownDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
        //  var updateMe = _clownDocuments.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
        //  // Update:
        //  updateMe.FirstName = "Bill";
        //  _clownDocuments.Update(updateMe);

        //  // Go find the updated record after reloading:
        //  _clownDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
        //  var updated = _clownDocuments.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
        //  Assert.True(updated.FirstName == "Bill");
        //}


        //[Fact(DisplayName = "Deletes a document with a serial PK")]
        //public void Deletes_Document_With_Serial_PK() {
        //  var newCustomer = new ClientDocument {
        //    Email = "rob@tekpub.com",
        //    FirstName = "Rob",
        //    LastName = "Conery"
        //  };
        //  _clownDocuments.Add(newCustomer);
        //  // Count after adding new:
        //  int initialCount = _clownDocuments.Count();
        //  var removed = _clownDocuments.Remove(newCustomer);

        //  // Reload, make sure everything was persisted:
        //  _clownDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
        //  // Count after removing and reloading:
        //  int finalCount = _clownDocuments.Count();
        //  Assert.True(finalCount < initialCount);
        //}


        //[Fact(DisplayName = "Bulk-Inserts new records as JSON documents with string key")]
        //public void Bulk_Inserts_Documents_With_String_PK() {
        //  int INSERT_QTY = 100;

        //  var addRange = new List<MonkeyDocument>();
        //  for (int i = 0; i < INSERT_QTY; i++) {
        //    addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
        //  }
        //  var inserted = _monkeyDocuments.Add(addRange);

        //  // Reload, make sure everything was persisted:
        //  _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));
        //  Assert.True(_monkeyDocuments.Count() == INSERT_QTY);
        //}


        //[Fact(DisplayName = "Bulk-Inserts new records as JSON documents with serial int key")]
        //public void Bulk_Inserts_Documents_With_Serial_PK() {
        //  int INSERT_QTY = 100;
        //  var bulkList = new List<ClientDocument>();
        //  for (int i = 0; i < INSERT_QTY; i++) {
        //    var newClientDocument = new ClientDocument {
        //      FirstName = "ClientDocument " + i,
        //      LastName = "Test",
        //      Email = "jatten@example.com"
        //    };
        //    bulkList.Add(newClientDocument);
        //  }
        //  _clownDocuments.Add(bulkList);

        //  // Reload, make sure everything was persisted:
        //  _clownDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));

        //  var last = _clownDocuments.Last();
        //  Assert.True(_clownDocuments.Count() == INSERT_QTY && last.ClientDocumentId >= INSERT_QTY);
        //}


        //[Fact(DisplayName = "Clears List and Store")]
        //public void Clears_List_and_Store() {
        //  int INSERT_QTY = 100;

        //  var addRange = new List<MonkeyDocument>();
        //  for (int i = 0; i < INSERT_QTY; i++) {
        //    addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
        //  }
        //  var inserted = _monkeyDocuments.Add(addRange);

        //  // Reload, make sure everything was persisted:
        //  _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));

        //  _monkeyDocuments.Clear();

        //  // Reload, make sure everything was persisted:
        //  _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));

        //  Assert.True(_monkeyDocuments.Count() == 0);
        //}
    }
}
