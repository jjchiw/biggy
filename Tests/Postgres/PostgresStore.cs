using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.Postgres;

namespace Tests.Postgres {
  [Trait("Postgres Store", "")]
  public class PostgresStore {
    string _connectionStringName = "chinookPG";
    IBiggyStore<Client> _biggyStore;
    PGStore<Client> _clientStore;

    public PostgresStore() {
      var context = new PGCache(_connectionStringName);

      // Build a table to play with from scratch each time:
      if (context.TableExists("client")) {
        context.DropTable("client");
      }
      var columnDefs = new List<string>();
      columnDefs.Add("client_id serial PRIMARY KEY NOT NULL");
      columnDefs.Add("last_name Text NOT NULL");
      columnDefs.Add("first_name Text NOT NULL");
      columnDefs.Add("email Text NOT NULL");

      context.CreateTable("client", columnDefs);
    }


    [Fact(DisplayName = "Initializes with Injected Cache")]
    public void Intialize_With_Injected_Context() {
      var cache = new PGCache(_connectionStringName);
      _clientStore = new PGStore<Client>(cache);
      Assert.True(_clientStore != null && _clientStore.Cache.DbTableNames.Count > 0);
    }


    [Fact(DisplayName = "Initializes with Connection String Name")]
    public void Intialize_With_Connection_String_Name() {
      _clientStore = new PGStore<Client>(_connectionStringName);
      Assert.True(_clientStore != null && _clientStore.Cache.DbTableNames.Count > 0);
    }


    [Fact(DisplayName = "IBiggyStore Adds a Record")]
    public void IBiggyStore_Adds_Record() {
      _biggyStore = new PGStore<Client>(_connectionStringName);
      var newClient = new Client() { LastName = "Atten", FirstName = "John", Email = "jatten@example.com" };
      _biggyStore.Add(newClient);
      Assert.True(newClient.ClientId > 0);
    }


    [Fact(DisplayName = "IBiggyStore Adds a Bunch of Records")]
    public void IBiggyStore_Adds_Many_Records() {
      _biggyStore = new PGStore<Client>(_connectionStringName);
      var insertThese = new List<Client>();

      for (int i = 0; i < 10; i++) {
        var newClient = new Client() { LastName = string.Format("LastName {0}", i), FirstName = "John", Email = "jatten@example.com" };
        insertThese.Add(newClient);
      }
      _biggyStore.Add(insertThese);
      var newClients = _biggyStore.Load();
      Assert.True(newClients.Count > 0);
    }



    [Fact(DisplayName = "IUpdateableBiggyStore Adds a Record")]
    public void IUpdateableBiggyStore_Updates_Record() {
      _biggyStore = new PGStore<Client>(_connectionStringName);
      var newClient = new Client() { LastName = "Atten", FirstName = "John", Email = "jatten@example.com" };
      _biggyStore.Add(newClient);

      // Stow the id so we can reload, then update (just to be SURE!!)
      int idToFind = newClient.ClientId;
      newClient = _biggyStore.Load().FirstOrDefault(c => c.ClientId == idToFind);
      newClient.FirstName = "John Paul";
      newClient.LastName = "Jones";
      _biggyStore.Update(newClient);

      var updatedClient = _biggyStore.Load().FirstOrDefault(c => c.ClientId == idToFind);
      Assert.True(updatedClient.LastName == "Jones");
    }


    [Fact(DisplayName = "IUpdateableBiggyStore Deletes a Record")]
    public void IUpdateableBiggyStore_Deletes_Record() {
      _biggyStore = new PGStore<Client>(_connectionStringName);
      var newClient = new Client() { LastName = "Atten", FirstName = "John", Email = "jatten@example.com" };
      _biggyStore.Add(newClient);

      // Stow the id so we can reload, then update (just to be SURE!!)
      int idToFind = newClient.ClientId;
      newClient = _biggyStore.Load().FirstOrDefault(c => c.ClientId == idToFind);

      var deleteMe = _biggyStore.Load().FirstOrDefault(c => c.ClientId == idToFind);
      _biggyStore.Remove(deleteMe);
      var clients = _biggyStore.Load();
      Assert.True(clients.Count == 0);
    }


    [Fact(DisplayName = "IUpdateableBiggyStore Deletes a Bunch of Records")]
    public void IUpdateableBiggyStore_Deletes_Many_Records() {
      _biggyStore = new PGStore<Client>(_connectionStringName);
      var insertThese = new List<Client>();

      for (int i = 0; i < 10; i++) {
        var newClient = new Client() { LastName = string.Format("LastName {0}", i), FirstName = "John", Email = "jatten@example.com" };
        insertThese.Add(newClient);
      }
      _biggyStore.Add(insertThese);
      var newClients = _biggyStore.Load();

      _biggyStore.Remove(newClients);
      newClients = _biggyStore.Load();
      Assert.True(newClients.Count == 0);
    }

    [Fact(DisplayName = "Deletes a Bunch of Records with a string key")]
    public void IBiggyStore_Deletes_Many_Records_With_String_PK() {
      IBiggyStore<Widget> widgetStore = new PGStore<Widget>(_connectionStringName);
      var insertThese = new List<Widget>();

      for (int i = 0; i < 10; i++) {
        var newWidget = new Widget() { SKU = "SKU " + i, Name = "Widget " + i, Price = Decimal.Parse(i.ToString()) };
        insertThese.Add(newWidget);
      }
      widgetStore.Add(insertThese);
      var newWidgets = widgetStore.Load();
      int insertedCount = newWidgets.Count();

      widgetStore.Remove(newWidgets);
      newWidgets = widgetStore.Load();
      Assert.True(insertedCount == 10 && newWidgets.Count() == 0);
    }


    [Fact(DisplayName = "Pulls things dynamically")]
    public void PullsThingsDynamically() {
      var list = new PGStore<dynamic>(_connectionStringName);
      var results = list.Query(@"select artist.name AS artist_name, track.name, track.unit_price 
                                   from artist inner join 
                                   album on artist.artist_id = album.artist_id inner join
                                   track on album.album_id = track.album_id
                                   where (artist.name = @0)", "AC/DC");
      Assert.True(results.Count() > 0);
    }



  }
}
