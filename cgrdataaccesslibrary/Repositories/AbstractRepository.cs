using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace cgrdataaccesslibrary.Repositories;

public class AbstractRepository<K, T> : IRepository<K, T> where T : class where K : notnull
{
    private readonly CGRContext _context;
    public AbstractRepository(CGRContext context)
    {
        _context = context;
    }
    public async Task<T> Create(T item)
    {
            _context.Set<T>().Add(item);
            await _context.SaveChangesAsync();
            return item;
    }

    public async Task<T> Delete(K key)
    {
     

            T? item = await Get(key);
            if(item == null)
            {
                throw new NotFoundException($"{typeof(T).Name} with key {key}");
            }
            _context.Set<T>().Remove(item);
            await _context.SaveChangesAsync();
            return item;
       
    }

    public async Task<T> Get(K key)
    {
        
            T? item = await _context.Set<T>().FindAsync(key);
            if(item == null)
            {
                throw new NotFoundException($"{typeof(T).Name} with key {key}");
            }
            return item;
       
    }

    public async Task<IEnumerable<T>> GetAll()
    {
       
            IEnumerable<T> list = await _context.Set<T>().ToListAsync();
            return list;
    }

    public async Task<T> Update(T item, K key)
    {
           T old = await Get(key);
        if(old == null)
        {
            throw new NotFoundException($"No updates as entity of {typeof(T).Name}");
        }
         _context.Set<T>().Update(item);
         await _context.SaveChangesAsync();
         return item;

       
    }
}
