using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Biggy
{
  public interface IBiggyStore<T> {
    List<T> Load();
    //void SaveAll(List<T> items);
    void Clear();
    T Add(T item);
    IList<T> Add(List<T> items);
    T Update(T item);
    T Remove(T item);
    IList<T> Remove(List<T> items);

	#if PCL
	Task<List<T>> LoadAsync();
	Task<bool> ClearAsync();     
	Task<T> AddAsync(T item);
	Task<IList<T>> AddAsync(List<T> items);
	Task<T> UpdateAsync(T item);
	Task<T> RemoveAsync(T item);
	Task<IList<T>> RemoveAsync(List<T> items);
	#endif
  }

  public interface IQueryableBiggyStore<T> : IBiggyStore<T> {
    IQueryable<T> AsQueryable();
  }
}