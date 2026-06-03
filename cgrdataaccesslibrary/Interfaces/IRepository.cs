namespace cgrdataaccesslibrary.Interfaces;

public interface IRepository<K,T> where T:class where K:notnull
{
    
    public Task<T> Create(T item);
    public Task<T> Get(K key);
    public Task<IEnumerable<T>> GetAll();
    public Task<T> Update(T item,K key);

    public Task<T> Delete(K key);

}
    
