﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;

namespace Tests {

  public class Property {
    public int PropertyDocumentId { get; set; }
    public string Name { get; set; }
  }

  public class Building {
    // for tests, both PropertyId and BuildingId should be considered
    // as participating in a composite PK:
    public int PropertyId { get; set; }
    public int BuildingId { get; set; }
    public string Name { get; set; }
  }

  public class PropertyDocument {
    [PrimaryKey(Auto: true)]
    public int PropertyDocumentId { get; set; }
    public string Name { get; set; }
  }

  public class BuildingDocument {
    [PrimaryKey (Auto: false)]
    public int PropertyId { get; set; }
    [PrimaryKey(Auto: false)]
    public int BuildingId { get; set; }
    public string Name { get; set; }
  }

  [DbTable("project")]
  public class Project {
    public int Id { get; set; }

    public string ProjectName { get; set; }
  }


  public class Film {
    [PrimaryKey(Auto: true)]
    public int Film_ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int ReleaseYear { get; set; }
    public int Length { get; set; }

    [FullText]
    public string FullText { get; set; }
  }


  public class Widget {
    [PrimaryKey(Auto: false)]
    public string SKU { get; set; }
    public string Name { get; set; }
    public Decimal Price { get; set; }
  }

  public class CompoundWidget {
    [PrimaryKey(Auto: false)]
    public string SKU { get; set; }
    [PrimaryKey(Auto: false)]
    public string Name { get; set; }
    public Decimal Price { get; set; }
  }

  public class OverrideWidget {
    public string SKU { get; set; }
    public string Name { get; set; }
    public Decimal Price { get; set; }

    public override bool Equals(object obj) {
      var w1 = (OverrideWidget)obj;
      return this.SKU == w1.SKU | ReferenceEquals(this, obj);
    }
  }


  public class MonkeyDocument {
    [PrimaryKey(Auto: false)]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }


  public class Client {
    [PrimaryKey(Auto: true)]
    public int ClientId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }

    public override bool Equals(object obj) {
      var c1 = (Client)obj;
      return this.ClientId == c1.ClientId | ReferenceEquals(this, obj);
    }
  }


  public class ClientDocument {
    [PrimaryKey(Auto: true)]
    public int ClientDocumentId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }


  class MismatchedClient {
    [PrimaryKey(Auto: true)]
    [DbColumn("CLient_Id")]
    public int Id { get; set; }
    [DbColumn("Last Name")]
    public string Last { get; set; }
    [DbColumn("first_name")]
    public string First { get; set; }
    [DbColumn("Email")]
    public string EmailAddress { get; set; }
  }
}
