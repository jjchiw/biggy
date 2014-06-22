using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Humanizer;
using Biggy.Mobile.WP8.Helpers;
using Windows.Storage;

namespace Biggy.JSON
{
    [JsonConverter(typeof(BiggyListSerializer))]
    public class JsonStore<T> : IBiggyStore<T> where T : new()
    {
        JsonSerializerSettings jsSettings;
        internal List<T> _items;
        public bool InMemory { get; set; }
        StorageFolder _dataFolder;
        StorageFolder _localFolder;
        string _dbFileName;
        string _dbName;

        public JsonStore(bool inMemory = false, string dbName = "")
        {
            this.InMemory = inMemory;
            if (String.IsNullOrWhiteSpace(dbName))
            {
                var thingyType = this.GetType().GenericTypeArguments[0].Name;
                _dbName = thingyType.Pluralize().ToLower();
            }
            else
            {
                _dbName = dbName.ToLower();
            }
            _dbFileName = _dbName + ".json";

            jsSettings = new JsonSerializerSettings();
            jsSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }

        private async Task CreateOrOpenFolder()
        {
            if (_localFolder != null && _dataFolder != null)
                return;

            _localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            _dataFolder = await _localFolder.CreateFolderAsync("data", CreationCollisionOption.OpenIfExists);
            await _dataFolder.CreateFileAsync(_dbFileName, CreationCollisionOption.OpenIfExists);

            return;
        }
       
        public async Task<T> AddItemAsync(T item)
        {
            await CreateOrOpenFolder();

            var json = string.Concat(JsonConvert.SerializeObject(item, jsSettings), Environment.NewLine);

            var bytes = Encoding.UTF8.GetBytes(json);
            using (Stream f = await _dataFolder.OpenStreamForWriteAsync(_dbFileName, CreationCollisionOption.OpenIfExists))
            {
                f.Seek(0, SeekOrigin.End);
                await f.WriteAsync(bytes, 0, bytes.Length);
            }

            _items.Add(item);
            return item;
        }

        public async Task<List<T>> TryLoadFileDataAsync(string path)
        {
            await CreateOrOpenFolder();

            List<T> result = new List<T>();
            StorageFile file = await _dataFolder.GetFileAsync(path);

            //format for the deserializer...
            var stream = await file.OpenStreamForReadAsync();

            var dataText = "";
            // Read the data.
            using (StreamReader streamReader = new StreamReader(stream))
            {
                dataText = streamReader.ReadToEnd();
            }

            string json = "[" + dataText.Replace(Environment.NewLine, ",") + "]";
            result = JsonConvert.DeserializeObject<List<T>>(json);

            _items = result.ToList();
            if (ReferenceEquals(_items, result))
            {
                throw new Exception("Yuck!");
            }
            return result;
        }

        public async Task<bool> FlushToDiskAsync()
        {
            await CreateOrOpenFolder();

            var completed = false;
            // Serialize json directly to the output stream
            using (Stream stream = await _dataFolder.OpenStreamForWriteAsync(_dbFileName, CreationCollisionOption.ReplaceExisting))
            using (var outstream = new StreamWriter(stream))
            {
                var writer = new JsonTextWriter(outstream);
                var serializer = JsonSerializer.CreateDefault();
                // Invoke custom serialization in BiggyListSerializer
                var biggySerializer = new BiggyListSerializer();
                biggySerializer.WriteJson(writer, _items, serializer);
                completed = true;
            }
            return completed;
        }

        public async Task<IList<T>> AddRangeAsync(List<T> items)
        {
            await CreateOrOpenFolder();

            using (Stream f = await _dataFolder.OpenStreamForWriteAsync(_dbFileName, CreationCollisionOption.OpenIfExists))
            {
                f.Seek(0, SeekOrigin.End);
                foreach (var item in items)
                {
                    var json = string.Concat(JsonConvert.SerializeObject(item, jsSettings), Environment.NewLine);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await f.WriteAsync(bytes, 0, bytes.Length);
                    _items.Add(item);
                }

            }

            return items;
        }

        public T Add(T item)
        {
            throw new NotImplementedException();
        }

        public Task<T> AddAsync(T item)
        {
            return AddItemAsync(item);
        }

        public IList<T> Add(List<T> items)
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> AddAsync(List<T> items)
        {
            return AddRangeAsync(items);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ClearAsync()
        {
            _items = new List<T>();
            return this.FlushToDiskAsync();
        }

        public List<T> Load()
        {
            return new List<T>();
        }

        public Task<List<T>> LoadAsync()
        {
            _items = new List<T>();
            var res = this.TryLoadFileDataAsync(_dbFileName);
            return res;
        }

        public void SaveAll(List<T> items)
        {
            throw new NotImplementedException();
        }

        public void SaveAllAsync(List<T> items)
        {
            throw new NotImplementedException();
        }
        
        public async virtual Task<T> UpdateItemAsync(T item)
        {
            var index = _items.IndexOf(item);
            if (index > -1)
            {
                _items.RemoveAt(index);
                _items.Insert(index, item);
                await this.FlushToDiskAsync();
            }
            return item;
        }

        public async virtual Task<T> RemoveItemAsync(T item)
        {
            _items.Remove(item);
            await this.FlushToDiskAsync();
            return item;
        }

        public async virtual Task<IList<T>> RemoveRangeAsync(List<T> items)
        {
            foreach (var item in items)
            {
                _items.Remove(item);
            }
            await this.FlushToDiskAsync();
            return items;
        }

        public IList<T> Remove(List<T> items)
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> RemoveAsync(List<T> items)
        {
            return RemoveRangeAsync(items);
        }

        public T Remove(T item)
        {
            throw new NotImplementedException();
        }

        public Task<T> RemoveAsync(T item)
        {
            return RemoveItemAsync(item);
        }

        public T Update(T item)
        {
            throw new NotImplementedException();
        }

        public Task<T> UpdateAsync(T item)
        {
            return UpdateItemAsync(item);
        }

    }
}
