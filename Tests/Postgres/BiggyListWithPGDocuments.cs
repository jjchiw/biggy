using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Biggy.Postgres;
using Xunit;
using Newtonsoft.Json;

namespace Tests.Postgres {

  [Trait("Biggy List With Postgres Document Store", "")]
  public class BiggyListWithPGDocuments {
    string _connectionStringName = "chinookPG";
    IBiggy<ClientDocument> _clientDocuments;
    IBiggy<MonkeyDocument> _monkeyDocuments;


    public BiggyListWithPGDocuments() {
      var _cache = new PGCache(_connectionStringName);
      // This one will be re-created automagically:
      if (_cache.TableExists("ClientDocuments")) {
        _cache.DropTable("ClientDocuments");
      }
      // This one will be re-created automagically:
      if (_cache.TableExists("MonkeyDocuments")) {
        _cache.DropTable("MonkeyDocuments");
      }
      _clientDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
      _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));
    }


    [Fact(DisplayName = "Creates a store with a serial PK if one doesn't exist")]
    public void Creates_Document_Table_With_Serial_PK_If_Not_Present() {
      Assert.True(_clientDocuments.Count() == 0);
    }


    [Fact(DisplayName = "Creates a store with a string PK if one doesn't exist")]
    public void Creates_Document_Table_With_String_PK_If_Not_Present() {
      Assert.True(_monkeyDocuments.Count() == 0);
    }


    [Fact(DisplayName = "Adds a document with a serial PK")]
    public void Adds_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      _clientDocuments.Add(newCustomer);
      Assert.Equal(1, _clientDocuments.Count());
    }


    [Fact(DisplayName = "Updates a document with a serial PK")]
    public void Updates_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      _clientDocuments.Add(newCustomer);
      int idToFind = newCustomer.ClientDocumentId;

      // Go find the new record after reloading:
      _clientDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
      var updateMe = _clientDocuments.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      // Update:
      updateMe.FirstName = "Bill";
      _clientDocuments.Update(updateMe);

      // Go find the updated record after reloading:
      _clientDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
      var updated = _clientDocuments.FirstOrDefault(cd => cd.ClientDocumentId == idToFind);
      Assert.True(updated.FirstName == "Bill");
    }


    [Fact(DisplayName = "Deletes a document with a serial PK")]
    public void Deletes_Document_With_Serial_PK() {
      var newCustomer = new ClientDocument {
        Email = "rob@tekpub.com",
        FirstName = "Rob",
        LastName = "Conery"
      };
      _clientDocuments.Add(newCustomer);
      // Count after adding new:
      int initialCount = _clientDocuments.Count();
      var removed = _clientDocuments.Remove(newCustomer);

      // Reload, make sure everything was persisted:
      _clientDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));
      // Count after removing and reloading:
      int finalCount = _clientDocuments.Count();
      Assert.True(finalCount < initialCount);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with string key")]
    public void Bulk_Inserts_Documents_With_String_PK() {
      int INSERT_QTY = 100;

      var addRange = new List<MonkeyDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = _monkeyDocuments.Add(addRange);

      // Reload, make sure everything was persisted:
      _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));
      Assert.True(_monkeyDocuments.Count() == INSERT_QTY);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with serial int key")]
    public void Bulk_Inserts_Documents_With_Serial_PK() {
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
      _clientDocuments.Add(bulkList);

      // Reload, make sure everything was persisted:
      _clientDocuments = new BiggyList<ClientDocument>(new PGDocumentStore<ClientDocument>(_connectionStringName));

      var last = _clientDocuments.Last();
      Assert.True(_clientDocuments.Count() == INSERT_QTY && last.ClientDocumentId >= INSERT_QTY);
    }


    [Fact(DisplayName = "Clears List and Store")]
    public void Clears_List_and_Store() {
      int INSERT_QTY = 100;

      var addRange = new List<MonkeyDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        addRange.Add(new MonkeyDocument { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = _monkeyDocuments.Add(addRange);

      // Reload, make sure everything was persisted:
      _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));

      _monkeyDocuments.Clear();

      // Reload, make sure everything was persisted:
      _monkeyDocuments = new BiggyList<MonkeyDocument>(new PGDocumentStore<MonkeyDocument>(_connectionStringName));

      Assert.True(_monkeyDocuments.Count() == 0);
    }

  }
}
