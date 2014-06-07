using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;
using System.Dynamic;
using Newtonsoft.Json;

namespace Biggy
{
  public abstract class BiggyDocumentStore<T> : IBiggyStore<T> where T : new()
  {
    public abstract T Insert(T item);
    public abstract List<T> BulkInsert(List<T> items);
    public abstract T Update(T item);
    public abstract T Delete(T item);
    public abstract List<T> Delete(List<T> items);
    protected abstract List<T> TryLoadData();
    public abstract BiggyRelationalStore<dynamic> getModel();

    public string[] FullTextFields { get; set; }
    public string[] LazyLoadingFields { get; set; }
    public BiggyRelationalStore<dynamic> Model { get; set; }
    public DbCache DbCache { get; set; }

    public DBTableMapping TableMapping  {
      get { return this.Model.TableMapping; }
      set { this.Model.TableMapping = value; }
    }

    public BiggyDocumentStore(DbCache dbCache) {
      SetLazyLoadingColumns();
      this.DbCache = dbCache;
      this.Model = this.getModel();
      this.TableMapping = this.getTableMappingForT();
      SetFullTextColumns();
      TryLoadData();
    }

    string _userDefinedTableName = "";
    public BiggyDocumentStore(DbCache dbCache, string tableName) {
      SetLazyLoadingColumns();
      _userDefinedTableName = tableName;
      this.DbCache = dbCache;
      this.Model = this.getModel();
      this.TableMapping = this.getTableMappingForT();
      SetFullTextColumns();
      TryLoadData();
    }

    public void CreateDocumentTableForT(List<string> columnDefs) {
      string columnDefinitions = string.Join(",", columnDefs.ToArray());
      var sql = string.Format("CREATE TABLE {0} ({1});", this.TableMapping.DelimitedTableName, columnDefinitions);
      this.Model.Execute(sql);
    }

    public DBTableMapping getTableMappingForT() {
      var result = new DBTableMapping(this.DbCache.DbDelimiterFormatString);
      result.DBTableName = this.DecideTableName();
      var pks = this.getPrimaryKeyForT();
      foreach (var pk in pks) {
        result.PrimaryKeyMapping.Add(pk);
        result.ColumnMappings.Add(pk);
      }
      result.ColumnMappings.Add("body", "body");
      result.ColumnMappings.Add("search", "search");

      foreach (var item in LazyLoadingFields){
          result.ColumnMappings.Add(item.ToLower(), item);
      }

      return result;
    }

    internal virtual string GetBaseName() {
      return typeof(T).Name;
    }

    string DecideTableName() {
      if(string.IsNullOrEmpty(_userDefinedTableName)) {
        //use the type name
        var baseName = this.GetBaseName();
        return Inflector.Inflector.Pluralize(baseName);
      }
      return _userDefinedTableName;
    }

    void SetFullTextColumns() {
      var foundProps = new T().LookForCustomAttribute(typeof(FullTextAttribute));
      this.FullTextFields = foundProps.Select(x => x.Name).ToArray();
    }

    void SetLazyLoadingColumns() {
        var foundProps = new T().LookForCustomAttribute(typeof(LazyLoadingAttribute));
        this.LazyLoadingFields = foundProps.Select(x => x.Name).ToArray();
    }

    List<DbColumnMapping> getPrimaryKeyForT() {
      List<DbColumnMapping> result = new List<DbColumnMapping>();
      string newTableName = this.DecideTableName();
      var baseName = this.GetBaseName();
      var acceptableKeys = new string[] { "ID", baseName + "ID" };
      //var props = typeof(T).GetProperties();

      var item = new T();
      var itemType = item.GetType();
      var props = itemType.GetProperties();
      
      // Check for custom attributes first - for doc stores, this should be 
      // the primary way to define keys:
      var foundProps = props.Where(p => p.GetCustomAttributes(false)
        .Any(a => a.GetType() == typeof(PrimaryKeyAttribute)));

      if (foundProps != null && foundProps.Count() > 0) {
        foreach (var pk in foundProps) {
          var attribute = pk.GetCustomAttributes(false).First(a => a.GetType() == typeof(PrimaryKeyAttribute));
          var pkAttribute = attribute as PrimaryKeyAttribute;
          //PrimaryKeyAttribute pkAttribute = pk.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).FirstOrDefault() as PrimaryKeyAttribute;
          var newMapping = new DbColumnMapping(this.DbCache.DbDelimiterFormatString);
          newMapping.TableName = newTableName;
          newMapping.ColumnName = pk.Name;
          newMapping.DataType = pk.PropertyType;
          newMapping.PropertyName = pk.Name;
          newMapping.IsPrimaryKey = true;
          newMapping.IsAutoIncementing = pkAttribute.IsAutoIncrementing;
          result.Add(newMapping);
        }
      } else {
        // No custom attributes were found. Do your best with column names:
        var conventionalKey = props.FirstOrDefault(x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) ??
            props.FirstOrDefault(x => x.Name.Equals(baseName + "ID", StringComparison.OrdinalIgnoreCase));

        var newMapping = new DbColumnMapping(this.DbCache.DbDelimiterFormatString);
        newMapping.DataType = typeof(int);
        newMapping.ColumnName = conventionalKey.Name;
        newMapping.PropertyName = conventionalKey.Name;
        newMapping.IsPrimaryKey = true;
        newMapping.IsAutoIncementing = newMapping.DataType == typeof(int);
        result.Add(newMapping);
      }
      if (result.Count == 0) {
        throw new InvalidOperationException("Can't tell what the primary key is. You can use ID, " + baseName + "ID, or specify with the PrimaryKey attribute");
      }
      return result;
    }

    protected ExpandoObject SetDataForDocument(T item) {
      
      var expando = new ExpandoObject();
      var dict = expando as IDictionary<string, object>;

      var itemProperties = item.ToExpando();
      var itemPropertiesDictionary = itemProperties as IDictionary<string, object>;
      foreach (var pk in this.TableMapping.PrimaryKeyMapping) {
        dict[pk.PropertyName] = itemPropertiesDictionary[pk.PropertyName];
      }
      dict["body"] = json;

      if (this.FullTextFields.Length > 0) {
        //get the data from the item passed in
        var itemdc = item.ToDictionary();
        var vals = new List<string>();
        foreach (var ft in this.FullTextFields) {
          var val = itemdc[ft] == null ? "" : itemdc[ft].ToString();
          vals.Add(val);
        }
        dict["search"] = string.Join(",", vals);
      }

      if (this.LazyLoadingFields.Length > 0) {
          //get the data from the item passed in
          var itemdc = item.ToDictionary();
          var vals = new List<string>();
          foreach (var ft in this.LazyLoadingFields)
          {
              dict[ft] = itemdc[ft] == null ? "" : JsonConvert.SerializeObject(itemdc[ft]);
              
          }
      }

      var json = JsonConvert.SerializeObject(item);
      dict["body"] = json;

      return expando;
    }


    public List<T> LoadAll() {
      var list = new List<T>();
      var results = this.Model.Query("select body from " + this.TableMapping.DelimitedTableName);//this.Model.All<T>().ToList();
      //our results are all dynamic - but all we care about is the body
      var sb = new StringBuilder();
      foreach (var item in results) {
        sb.AppendFormat("{0},", item.body);
      }
      // Can't take a substring of a zero-length string:
      if (sb.Length > 0) {
        var scrunched = sb.ToString();
        var stripped = scrunched.Substring(0, scrunched.Length - 1);
        var json = string.Format("[{0}]", stripped);
        list = JsonConvert.DeserializeObject<List<T>>(json);
      }

      return list;
    }

    List<T> IBiggyStore<T>.Load() {
      return this.LoadAll();
    }

    void IBiggyStore<T>.Clear() {
      var store = this.Model as IBiggyStore<dynamic>;
      store.Clear();
    }

    T IBiggyStore<T>.Add(T item) {
      return this.Insert(item);
    }

    IList<T> IBiggyStore<T>.Add(List<T> items) {
      return this.BulkInsert(items.ToList());
    }

    T IBiggyStore<T>.Update(T item) {
      return this.Update(item);
    }

    T IBiggyStore<T>.Remove(T item) {
      return this.Delete(item);
    }

    IList<T> IBiggyStore<T>.Remove(List<T> items) {
      return this.Delete(items.ToList());
    }
  }
}
