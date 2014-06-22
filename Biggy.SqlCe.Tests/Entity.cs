using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.SqlCe.Tests {
  class Album {
	[PrimaryKey(true), DbColumn("AlbumId")]
    public int Id { get; set; }
    public string Title { get; set; }
    public int ArtistId { get; set; }
  }

  class Artist {
	[PrimaryKey(true)]
    public int ArtistId { get; set; }
    public string Name { get; set; }
  }

  class Genre {
    [DbColumn("GenreId")]
    public int Id { get; set; }
    public string Name { get; set; }
  }

  class Employee {
	[PrimaryKey(true)]
    public int EmployeeId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Title { get; set; }
    public int ReportsTo { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime HireDate { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }
    public string Phone { get; set; }
    public string Fax { get; set; }
    public string Email { get; set; }

  }

  class Client {
	[PrimaryKey(true)]
    public int ClientId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }

  class ClientDocument {
	[PrimaryKey(true)]
    public int ClientDocumentId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
  }

  class MonkeyDocument {
    [PrimaryKey(false)]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }
}
