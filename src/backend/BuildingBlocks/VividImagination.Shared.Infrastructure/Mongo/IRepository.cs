using System.Linq.Expressions;
using MongoDB.Driver;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Infrastructure.Mongo;

public interface IReadRepository<TEntity, in TId>
    where TEntity : class, IAggregate<TId>
{
    Task<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> RawQueryAsync(
        string query,
        CancellationToken cancellationToken = default,
        params object[] queryParams);
    
    Task<IPagedList<TEntity>> FindPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default);
        
    Task<IPagedList<TEntity>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default);
}

public interface IWriteRepository<TEntity, in TId>
    where TEntity : class, IAggregate<TId>
{
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity, in TId> :
    IReadRepository<TEntity, TId>,
    IWriteRepository<TEntity, TId>,
    IAggregationRepository<TEntity, TId>,
    IDisposable
    where TEntity : class, IAggregate<TId>;
