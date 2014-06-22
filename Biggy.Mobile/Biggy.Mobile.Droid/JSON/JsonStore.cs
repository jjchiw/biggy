using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Humanizer;
using Biggy;

namespace Biggy.JSON
{
	[JsonConverter (typeof(BiggyListSerializer))]
	public class JsonStore<T> : IBiggyStore<T> where T : new()
	{
		public string DbDirectory { get; set; }
		public bool InMemory { get; set; }
		public string DbFileName { get; set; }
		public string DbName { get; set; }
		internal List<T> _items;

		public string DbPath {
			get {
				return Path.Combine (DbDirectory, DbFileName);
			}
		}

		public bool HasDbFile {
			get {
				return File.Exists (DbPath);
			}
		}

		JsonSerializerSettings jsSettings;

		public JsonStore (bool inMemory = false, string dbName = "")
		{
			this.InMemory = inMemory;
			if (String.IsNullOrWhiteSpace (dbName)) {
				var thingyType = this.GetType ().GenericTypeArguments [0].Name;
				this.DbName = thingyType.Pluralize ().ToLower ();
			} else {
				this.DbName = dbName.ToLower ();
			}
			this.DbFileName = this.DbName + ".json";

			this.SetDataDirectory (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments));

			jsSettings = new JsonSerializerSettings ();
			jsSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
		}

		public void SetDataDirectory (string dbPath)
		{
			var dataDir = dbPath;
			dataDir = Path.Combine (dbPath, "data");
			if (!Directory.Exists (dataDir))
				Directory.CreateDirectory (dataDir);
			this.DbDirectory = dataDir;
		}

		public List<T> TryLoadFileData (string path)
		{
			List<T> result = new List<T> ();
			if (File.Exists (path)) {
				//format for the deserializer...
				var json = "[" + File.ReadAllText (path).Replace (Environment.NewLine, ",") + "]";
				result = JsonConvert.DeserializeObject<List<T>> (json);
			}
			_items = result.ToList ();
			if (ReferenceEquals (_items, result)) {
				throw new Exception ("Yuck!");
			}
			return result;
		}

		public T AddItem (T item)
		{
			var json = JsonConvert.SerializeObject (item, jsSettings);
			//append the to the file
			using (var writer = File.AppendText (this.DbPath)) {
				writer.WriteLine (json);
			}
			_items.Add (item);
			return item;
		}

		public List<T> AddRange (List<T> items)
		{
			//append the to the file
			using (var writer = File.AppendText (this.DbPath)) {
				foreach (var item in items) {
					var json = JsonConvert.SerializeObject (item, jsSettings);
					writer.WriteLine (json);
					_items.Add (item);
				}
			}
			return items;
		}

		public virtual T UpdateItem (T item)
		{
			var index = _items.IndexOf (item);
			if (index > -1) {
				_items.RemoveAt (index);
				_items.Insert (index, item);
				this.FlushToDisk ();
			}
			return item;
		}

		public virtual T RemoveItem (T item)
		{
			_items.Remove (item);
			this.FlushToDisk ();
			return item;
		}

		public virtual List<T> RemoveRange (List<T> items)
		{
			foreach (var item in items) {
				_items.Remove (item);
			}
			this.FlushToDisk ();
			return items;
		}

		public bool FlushToDisk ()
		{
			var completed = false;
			// Serialize json directly to the output stream
			using (var outstream = new StreamWriter (this.DbPath)) {
				var writer = new JsonTextWriter (outstream);
				var serializer = JsonSerializer.CreateDefault ();
				// Invoke custom serialization in BiggyListSerializer
				var biggySerializer = new BiggyListSerializer ();
				biggySerializer.WriteJson (writer, _items, serializer);
				completed = true;
			}
			return completed;
		}

		//public bool FlushToDisk() {
		//  var completed = false;
		//  // Serialize json directly to the output stream
		//  using (var outstream = new StreamWriter(this.DbPath)) {
		//    var writer = new JsonTextWriter(outstream);
		//    var serializer = JsonSerializer.CreateDefault();
		//    // Invoke custom serialization in BiggyListSerializer
		//    serializer.Serialize(writer, _items);
		//    completed = true;
		//  }
		//  return completed;
		//}

		#region IBiggyStore implementation

		public List<T> Load ()
		{
			_items = new List<T> ();
			return this.TryLoadFileData (this.DbPath);
		}

		public Task<List<T>> LoadAsync ()
		{
			return Task.Run<List<T>> (() => Load());
		}

		public void SaveAll (List<T> items)
		{
			throw new NotImplementedException ();
		}

		public void SaveAllAsync (List<T> items)
		{
			throw new NotImplementedException ();
		}

		public void Clear ()
		{
			_items = new List<T> ();
			this.FlushToDisk ();
		}

		public Task<bool> ClearAsync ()
		{
			return Task.Run<bool> (() => FlushToDisk());
		}

		public T Add (T item)
		{
			return this.AddItem (item);
		}

		public Task<T> AddAsync (T item)
		{
			return Task.Run<T> (() => Add (item));
		}

		public IList<T> Add (List<T> items)
		{
			return this.AddRange (items);
		}

		public Task<IList<T>> AddAsync (List<T> items)
		{
			return Task.Run<IList<T>> (() => Add (items));
		}

		#endregion

		#region IUpdateableBiggyStore

		public T Update (T item)
		{
			return this.UpdateItem (item);
		}

		public Task<T> UpdateAsync (T item)
		{
			return Task.Run<T> (() => Update (item));
		}

		public T Remove (T item)
		{
			return this.RemoveItem (item);
		}

		public Task<T> RemoveAsync (T item)
		{
			return Task.Run<T> (() => Remove (item));
		}

		public IList<T> Remove (List<T> items)
		{
			return this.RemoveRange (items);
		}

		public Task<IList<T>> RemoveAsync (List<T> items)
		{
			return Task.Run<IList<T>> (() => Remove (items));
		}

		#endregion

		public IQueryable<T> AsQueryable ()
		{
			return _items.AsQueryable ();
		}
	}
}
