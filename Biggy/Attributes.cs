using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {

  public class FullTextAttribute : Attribute { }
  public class PrimaryKeyAttribute : Attribute {
    public bool IsAutoIncrementing { get; private set; }
    public PrimaryKeyAttribute(bool Auto = true) {
      this.IsAutoIncrementing = Auto;
    }
  }

  public class DbColumnAttribute : Attribute {
    public string Name { get; protected set; }
    public DbColumnAttribute(string name) {
      this.Name = name;
    }
  }

  public class DbTableAttribute : Attribute {
    public string Name { get; protected set; }
    public DbTableAttribute(string name) {
      this.Name = name;
    }
  }

  public class LazyLoadingAttribute : Attribute {
      public int FirstLimit { get; protected set; }
      public bool IsDocument { get; protected set; }
      public bool Reverse { get; set; }
      public LazyLoadingAttribute(bool IsDocument = false, int FirstLimit  = 10, bool Reverse = false) {
          this.FirstLimit = FirstLimit;
          this.IsDocument = IsDocument;
          this.Reverse = Reverse;
      }
  }

}
