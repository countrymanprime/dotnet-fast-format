public class Box<T> where T : class, new() { }

public class Repository<TEntity, TKey> :
    IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{ }
