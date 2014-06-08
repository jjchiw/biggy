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
        IList<T> Remove(List<T> items);
        T Add(T item);
        IList<T> Add(List<T> items);
        bool InMemory { get; set; }

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

    public interface ILazyLoadingCollection<T> : IEnumerable<T>
    {
        void Remove(T item);
        void Remove(List<T> items);
        void Add(T item);
        void Add(List<T> items);
        void Load(BiggyRelationalStore<dynamic> store, string property, int skip, int take, object parent, params object[] args);
    }
}