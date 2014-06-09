using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.Postgres;

namespace Tests.Postgres {
  [Trait("PG Document Store", "")]
  public class PostgresDocumentStore {
    string _connectionStringName = "chinookPG";
    IBiggyStore<ClientDocument> clientDocs;
    IBiggyStore<MonkeyDocument> monkeyDocs;
    PGCache _cache;

    public PostgresDocumentStore() {
      _cache = new PGCache(_connectionStringName);

      // Build a table to play with from scratch each time:

      if (_cache.TableExists("ClientDocuments")) {
        _cache.DropTable("ClientDocuments");
      }
      if (_cache.TableExists("MonkeyDocuments")) {
        _cache.DropTable("MonkeyDocuments");
      }
      clientDocs = new PGDocumentStore<ClientDocument>(_connectionStringName);
      monkeyDocs = new PGDocumentStore<MonkeyDocument>(_connectionStringName);
    }


    [Fact(DisplayName = "Creates a store with a serial PK if one doesn't exist")]
    public void Creates_Document_Table_With_Serial_PK_If_Not_Present() {
      Assert.True(clientDocs.Load().Count() == 0);
    }


    [Fact(DisplayName = "Creates a store with a string PK if one doesn't exist")]
    public void Creates_Document_Table_With_String_PK_If_Not_Present() {
      Assert.True(monkeyDocs.Load().Count() == 0);
    }

    [Fact(DisplayName = "Adds a document with a serial PK")]
    public void Adds_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };

      IBiggyStore<ClientDocument> docStore = clientDocs as IBiggyStore<ClientDocument>;
      docStore.Add(newCustomer);
      docStore.Load();
      Assert.Equal(1, docStore.Load().Count());
    }

    [Fact(DisplayName = "Updates a document with a serial PK")]
    public void Updates_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      clientDocs.Add(newCustomer);
      int idToFind = newCustomer.ClientDocumentId;

      // Go find the new record after reloading:
      var updateMe = clientDocs.Load().FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      // Update:
      updateMe.FirstName = "Bill";
      clientDocs.Update(updateMe);
      // Go find the updated record after reloading:
      var updated = clientDocs.Load().FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      Assert.True(updated.FirstName == "Bill");
    }


    [Fact(DisplayName = "Deletes a document with a serial PK")]
    public void Deletes_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      clientDocs.Add(newCustomer);
      // Count after adding new:
      int initialCount = clientDocs.Load().Count();
      var removed = clientDocs.Remove(newCustomer);
      // Count after removing and reloading:
      int finalCount = clientDocs.Load().Count();
      Assert.True(finalCount < initialCount);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with string key")]
    public void Bulk_Inserts_Documents_With_String_PK() {
      int INSERT_QTY = 100;

      var addRange = new List<MonkeyDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }

      monkeyDocs.Add(addRange);
      var inserted = monkeyDocs.Load();
      Assert.True(inserted.Count() == INSERT_QTY);
    }

    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with serial int key")]
    void Bulk_Inserts_Documents_With_Serial_PK() {
      int INSERT_QTY = 100;
      var bulkList = new List<ClientDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        var newClientDocument = new ClientDocument {
          FirstName = "ClientDocument " + i,
          LastName = "Test",
          Email = "jatten@example.com"
        };
        bulkList.Add(newClientDocument);
      }
      clientDocs.Add(bulkList);

      var inserted = clientDocs.Load();
      Assert.True(inserted.Count() == INSERT_QTY);
    }

    [Fact(DisplayName = "Deletes a range of documents with serial key")]
    void Deletes_Range_of_Documents_With_Serial_PK() {
      int INSERT_QTY = 100;
      var bulkList = new List<ClientDocument>();
      for (int i = 1; i <= INSERT_QTY; i++) {
        var newBuildingDocument = new ClientDocument {
          FirstName = "ClientDocument " + i,
          LastName = "Test",
          Email = "jatten@example.com"
        };
        bulkList.Add(newBuildingDocument);
      }
      clientDocs.Add(bulkList);

      var inserted = clientDocs.Load();
      int insertedCount = inserted.Count;

      var deleteUs = inserted.Where(b => b.ClientDocumentId > 50);
      clientDocs.Remove(deleteUs.ToList());
      var remaining = clientDocs.Load();
      Assert.True(insertedCount > remaining.Count && remaining.Count == 50);
    }

    [Fact(DisplayName = "Deletes a range of documents with string key")]
    void Deletes_Range_of_Documents_With_String_PK() {
      int INSERT_QTY = 100;
      var bulkList = new List<MonkeyDocument>();
      for (int i = 1; i <= INSERT_QTY; i++) {
        var newMonkeyDocument = new MonkeyDocument {
          Name = "MonkeyDocument " + i,
          Birthday = DateTime.Now,
          Description = "This is Monkey number " + i
        };
        bulkList.Add(newMonkeyDocument);
      }
      monkeyDocs.Add(bulkList);

      var inserted = monkeyDocs.Load();
      int insertedCount = inserted.Count;

      var deleteUs = monkeyDocs.Remove(inserted);
      var remaining = monkeyDocs.Load();
      Assert.True(insertedCount > remaining.Count && remaining.Count == 0);
    }

  }
}
