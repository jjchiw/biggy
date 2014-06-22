using System;
using System.Collections.Generic;
using System.Linq;

namespace Biggy
{
    public interface IBiggy<T> : IEnumerable<T>
    {
        void Clear();
        int Count();
        T Update(T item);
        T Remove(T item);
        List<T> Remove(List<T> items);
        T Add(T item);
        List<T> Add(List<T> items);
        IQueryable<T> AsQueryable();

		event EventHandler<IBiggyEventArgs<T>> ItemRemoved;
		event EventHandler<IBiggyEventArgs<T>> ItemAdded;
		event EventHandler<IBiggyEventArgs<T>> ItemsAdded;

		event EventHandler<IBiggyEventArgs<T>> Changed;
		event EventHandler<IBiggyEventArgs<T>> Loaded;
		event EventHandler<IBiggyEventArgs<T>> Saved;
    }
}