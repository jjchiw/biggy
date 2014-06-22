using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if !PCL
using Biggy.Extensions;
#endif

namespace Biggy {
  public class BiggyList<T> : IBiggy<T> where T : new() {
    private readonly IBiggyStore<T> _store;
    private List<T> _items;

    bool _inMemory;
    public bool InMemory {
      get {
        if (_store == null) {
          return true;
        } else {
          return _inMemory;
        }
      }
      set {
        if (_store == null) {
          _inMemory = true;
        } else {
          _inMemory = value;
        }
      }
    }
    
    public BiggyList(IBiggyStore<T> store, bool inMemory = false) {
      _store = store;
      _items = (_store != null) ? _store.Load() : new List<T>();
      this.InMemory = inMemory;
    }

	#if !PCL
    public BiggyList(bool inMemory = false) {

      // Unless the in-memory flag is set, initialize with JSON store:
      if (!inMemory) {
        _store = new JSON.JsonStore<T>();
      }
      _items = (_store != null) ? _store.Load() : new List<T>();
      this.InMemory = inMemory;
    }
	#endif


	#if PCL
	public async Task<bool> LoadItemsAsync()
	{
		_items = await _store.LoadAsync();
		return true;
	}

	public bool LoadItems()
	{
		_items =_store.Load();
		return true;
	}
	#endif

    public virtual IEnumerator<T> GetEnumerator() {
      return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    public virtual void Clear() {
      if (_store != null && !this.InMemory) {
        _store.Clear();
      }
      _items.Clear();
      Fire(Changed, items: null);
    }

    public virtual int Count() {
      return _items.Count;
    }

    public virtual T Update(T item) {
      T itemFromList = default(T);
      if (!_items.Contains(item)) {
        // Figure out what to do here. Retreive Key From Store and evaluate?
        throw new InvalidOperationException(
          @"The list does not contain a reference to the object passed as an argument. 
          Make sure you are passing a valid reference, or override Equals on the type being passed.");
      } else {
        itemFromList = _items.ElementAt(_items.IndexOf(item));
        if (!ReferenceEquals(itemFromList, item)) {
          //// The items are "equal" but do not refer to the same instance. 
          //// Somebody overrode Equals on the type passed as an argument. Replace:
          int index = _items.IndexOf(item);
          _items.RemoveAt(index);
          _items.Insert(index, item);
        }
        // From here forward, the item passed in refers to the item in the list. 
      }
      if (_store != null && !this.InMemory) {
        _store.Update(item);
      } 
      Fire(Changed, item: item);
      return item;
    }

    public virtual T Remove(T item) {
      _items.Remove(item);
      if (_store != null && !this.InMemory) {
        _store.Remove(item);
      }
      Fire(ItemRemoved, item: item);
      return item;
    }

    public IList<T> Remove(List<T> items) {
      foreach (var item in items) {
        _items.Remove(item);
      }
      if (_store != null && !this.InMemory) {
        _store.Remove(items);
      }
      Fire(ItemsRemoved, items: items);
      return items;
    }

    public virtual T Add(T item) {
      if (_store != null && !this.InMemory) {
        _store.Add(item);
      }
      _items.Add(item);
      Fire(ItemAdded, item: item);
      return item;
    }

    public virtual IList<T> Add(List<T> items) {
      if (_store != null && !this.InMemory) {
        _store.Add(items);
      }
      _items.AddRange(items);
      Fire(ItemsAdded, items: items);
      return items;
    }

    //public virtual IQueryable<T> AsQueryable() {
    //  return _queryableStore != null && !this.InMemory ? _queryableStore.AsQueryable() : _items.AsQueryable();
    //}

    protected virtual void Fire(EventHandler<BiggyEventArgs<T>> @event, T item = default(T), IList<T> items = null) {
      if (@event != null) {
        var args = new BiggyEventArgs<T> { Item = item, Items = items };
        @event(this, args);
      }
    }

    public event EventHandler<BiggyEventArgs<T>> ItemRemoved;
    public event EventHandler<BiggyEventArgs<T>> ItemsRemoved;
    public event EventHandler<BiggyEventArgs<T>> ItemAdded;
    public event EventHandler<BiggyEventArgs<T>> ItemsAdded;

    public event EventHandler<BiggyEventArgs<T>> Changed;
    public event EventHandler<BiggyEventArgs<T>> Loaded;
    public event EventHandler<BiggyEventArgs<T>> Saved;
  }
}