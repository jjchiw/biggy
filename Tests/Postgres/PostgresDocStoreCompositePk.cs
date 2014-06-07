using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Biggy;
using Biggy.Postgres;

namespace Tests.Postgres {
  [Trait("PG Document Store with Composite PK", "")]
  public class PostgresDocStoreCompositePk {
        string _connectionStringName = "chinookPG";

    IBiggyStore<PropertyDocument> propertyDocs;
    IBiggyStore<BuildingDocument> buildingDocs;

    public PostgresDocStoreCompositePk() {
      var cache = new PGCache(_connectionStringName);

      // Build a table to play with from scratch each time:
      if (cache.TableExists("PropertyDocuments")) {
        cache.DropTable("PropertyDocuments");
      }
      if (cache.TableExists("building_documents")) {
        cache.DropTable("building_documents");
      }
      propertyDocs = new PGDocumentStore<PropertyDocument>(_connectionStringName);
      buildingDocs = new PGDocumentStore<BuildingDocument>(_connectionStringName);
    }


    [Fact(DisplayName = "Creates a store with a composite PK if one doesn't exist")]
    public void Creates_Document_Table_With_Composite_PK_If_Not_Present() {
      Assert.True(buildingDocs.Load().Count() == 0);
    }


    [Fact(DisplayName = "Adds a document with a composite PK")]
    public void Adds_Document_With_Composite_PK() {
      var newBuilding = new BuildingDocument {
        PropertyId = 1,
        BuildingId = 1,
        Name = "Building 1"
      };

      buildingDocs.Add(newBuilding);
      var fetchBuilding = buildingDocs.Load().First(); 
      Assert.True(fetchBuilding.Name == "Building 1");
    }

    [Fact(DisplayName = "Updates a document with a composite PK")]
    public void Updates_Document_With_Composite_PK() {
      var newBuilding = new BuildingDocument {
        PropertyId = 1,
        BuildingId = 1,
        Name = "Building 1"
      };
      buildingDocs.Add(newBuilding);
      int propertyId = newBuilding.PropertyId;
      int buildingId = newBuilding.BuildingId;
      // Go find the new record after reloading:

      var updateMe = buildingDocs.Load().FirstOrDefault(b => b.PropertyId == propertyId && b.BuildingId == buildingId);
      // Update:
      updateMe.Name = "Updated Building 1";
      buildingDocs.Update(updateMe);
      // Go find the updated record after reloading:
      var updated = buildingDocs.Load().FirstOrDefault(b => b.PropertyId == propertyId && b.BuildingId == buildingId);
      Assert.True(updateMe.Name == "Updated Building 1");
    }


    [Fact(DisplayName = "Deletes a document with a composite PK")]
    public void Deletes_Document_With_Composite_PK() {
      var newBuilding = new BuildingDocument {
        PropertyId = 1,
        BuildingId = 1,
        Name = "Building 1"
      };
      buildingDocs.Add(newBuilding);
      // Count after adding new:
      int initialCount = buildingDocs.Load().Count();
      var removed = buildingDocs.Remove(newBuilding);
      // Count after removing and reloading:
      int finalCount = buildingDocs.Load().Count();
      Assert.True(finalCount < initialCount);
    }


    [Fact(DisplayName = "Bulk-Inserts new records as JSON documents with composite key")]
    void Bulk_Inserts_Documents_With_Serial_PK() {
      int INSERT_QTY = 100;
      var bulkList = new List<BuildingDocument>();
      for (int i = 0; i < INSERT_QTY; i++) {
        var newBuildingDocument = new BuildingDocument {
          PropertyId = 1,
          BuildingId = i,
          Name = "Building " + i
        };
        bulkList.Add(newBuildingDocument);
      }
      buildingDocs.Add(bulkList);

      var inserted = buildingDocs.Load();
      Assert.True(inserted.Count() == INSERT_QTY);
    }

    [Fact(DisplayName = "Deletes a range of documents with composite key")]
    void Deletes_Range_of_Documents_With_Composite_PK() {
      int INSERT_QTY = 100;
      var bulkList = new List<BuildingDocument>();
      for (int i = 1; i <= INSERT_QTY; i++) {
        var newBuildingDocument = new BuildingDocument {
          PropertyId = 1,
          BuildingId = i,
          Name = "Building " + i
        };
        bulkList.Add(newBuildingDocument);
      }
      buildingDocs.Add(bulkList);

      var inserted = buildingDocs.Load();
      int insertedCount = inserted.Count;

      var deleteUs = inserted.Where(b => b.BuildingId > 50);
      buildingDocs.Remove(deleteUs.ToList());
      var remaining = buildingDocs.Load();
      Assert.True(insertedCount > remaining.Count && remaining.Count == 50);
    }
  }
}
