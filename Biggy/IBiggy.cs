﻿using System;
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

        event EventHandler<BiggyEventArgs<T>> BeforeItemRemoved;
        event EventHandler<BiggyEventArgs<T>> BeforeItemAdded;
        event EventHandler<BiggyEventArgs<T>> BeforeItemsAdded;

        event EventHandler<BiggyEventArgs<T>> BeforeChanged;
    }

    public interface IQueryableBiggyList<T> : IBiggy<T> {
      IQueryable<T> AsQueryable();
    }

    public interface ILazyLoadingCollection
    {
        List<dynamic> Added { get; set; }
        List<dynamic> Removed { get; set; }
    }
}