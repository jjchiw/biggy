using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Biggy
{
    public interface IBiggy<T> : IEnumerable<T>
    {
        void Clear();
        int Count();
        T Update(T item);
        T Remove(T item);
        IList<T> Remove(List<T> items);
        T Add(T item);
        IList<T> Add(List<T> items);
        bool InMemory { get; set; }

		#if PCL
		bool LoadItems ();
		Task<bool> LoadItemsAsync ();
		#endif

        event EventHandler<BiggyEventArgs<T>> ItemRemoved;
        event EventHandler<BiggyEventArgs<T>> ItemAdded;
        event EventHandler<BiggyEventArgs<T>> ItemsAdded;

        event EventHandler<BiggyEventArgs<T>> Changed;
        event EventHandler<BiggyEventArgs<T>> Loaded;
        event EventHandler<BiggyEventArgs<T>> Saved;
    }

    public interface IQueryableBiggyList<T> : IBiggy<T> {
      IQueryable<T> AsQueryable();
    }
}