using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy
{
    public class LazyLoadingCollection<T> : IBiggy<T>, ILazyLoadingCollection where T : new()
    {
        IBiggy<T> _list;

        public List<dynamic> Added { get; set; }
        public List<dynamic> Removed { get; set; }

        public LazyLoadingCollection()
        {
            _list = new BiggyList<T>(true);
            Added = new List<dynamic>();
            Removed = new List<dynamic>();
        }

        public void Clear()
        {
            _list.Clear();
        }

        public int Count()
        {
            return _list.Count();
        }

        public T Update(T item)
        {
            throw new NotSupportedException();
        }

        public T Remove(T item)
        {
            //Verificar que existe 
            Removed.Add(item);
            return _list.Remove(item);
        }

        public IList<T> Remove(List<T> items)
        {
            foreach (dynamic item in items)
            {
                Removed.Add(item);
            }
            return _list.Remove(items);
        }

        public T Add(T item)
        {
            Added.Add(item);
            return _list.Add(item);
        }

        public IList<T> Add(List<T> items)
        {
            foreach (dynamic item in items)
            {
                Added.Add(item);
            }
            
            return _list.Add(items);
        }

        public bool InMemory
        {
            get
            {
                return _list.InMemory;
            }
            set
            {
                _list.InMemory = value;
            }
        }

        public event EventHandler<BiggyEventArgs<T>> ItemRemoved;

        public event EventHandler<BiggyEventArgs<T>> ItemAdded;

        public event EventHandler<BiggyEventArgs<T>> ItemsAdded;

        public event EventHandler<BiggyEventArgs<T>> Changed;

        public event EventHandler<BiggyEventArgs<T>> Loaded;

        public event EventHandler<BiggyEventArgs<T>> Saved;

        public event EventHandler<BiggyEventArgs<T>> BeforeItemRemoved;

        public event EventHandler<BiggyEventArgs<T>> BeforeItemAdded;

        public event EventHandler<BiggyEventArgs<T>> BeforeItemsAdded;

        public event EventHandler<BiggyEventArgs<T>> BeforeChanged;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
