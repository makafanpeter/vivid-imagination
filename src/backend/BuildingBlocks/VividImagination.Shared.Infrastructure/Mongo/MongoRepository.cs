using System.Linq.Expressions;
using MongoDB.Driver;
using VividImagination.Shared.Domain.Entities;
using VividImagination.Shared.Infrastructure.Mongo.Extensions;

namespace VividImagination.Shared.Infrastructure.Mongo;

public class MongoRepository<TEntity, TId>(IMongoCollection<TEntity> collection) : IMongoRepository<TEntity, TId>
    where TEntity : class, IAggregate<TId>
{
    protected readonly IMongoCollection<TEntity> DbSet = collection;

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static SortDefinition<TEntity>? ResolveDefaultSort(SortDefinition<TEntity>? sort)
    {
        if (sort != null) return sort;

        return typeof(IAuditable).IsAssignableFrom(typeof(TEntity))
            ? Builders<TEntity>.Sort.Descending(nameof(IAuditable.CreatedOn))
            : null;
    }

    // ---------------------------------------------------------------------------
    // Read
    // ---------------------------------------------------------------------------

    public async Task<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default)
        => await FindOneAsync(e =>  e.Id.Equals(id), cancellationToken);

    public async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.Find(predicate).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Find(predicate);

        sort = ResolveDefaultSort(sort);
        if (sort != null) query = query.Sort(sort);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = DbSet.Find(FilterDefinition<TEntity>.Empty);

        var sort = ResolveDefaultSort(null);
        if (sort != null) query = query.Sort(sort);

        return await query.ToListAsync(cancellationToken);
    }
    

    public async Task<IReadOnlyList<TEntity>> RawQueryAsync(
        string query,
        CancellationToken cancellationToken = default,
        params object[] queryParams)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));

        var bsonQuery = MongoDB.Bson.BsonDocument.Parse(query);

        for (var i = 0; i < queryParams.Length; i++)
        {
            var paramName = $"@{i}";
            if (bsonQuery.Contains(paramName))
                bsonQuery[paramName] = MongoDB.Bson.BsonValue.Create(queryParams[i]);
        }

        var cursor = await DbSet.FindAsync(bsonQuery, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IPagedList<TEntity>> FindPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Find(predicate);

        sort = ResolveDefaultSort(sort);
        if (sort != null) query = query.Sort(sort);

        return await query.ToPagedListAsync(pageIndex, pageSize, cancellationToken);
    }

    public async Task<IPagedList<TEntity>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        SortDefinition<TEntity>? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Find(FilterDefinition<TEntity>.Empty);

        sort = ResolveDefaultSort(sort);
        if (sort != null) query = query.Sort(sort);

        return await query.ToPagedListAsync(pageIndex, pageSize, cancellationToken);
    }

    // ---------------------------------------------------------------------------
    // Write
    // ---------------------------------------------------------------------------

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is IAuditable auditable)
        {
            auditable.CreatedOn = DateTimeOffset.UtcNow;
            auditable.UpdatedOn = null;
        }

        if (entity is IVersionable versionable)
            versionable.Version = 1;

        await DbSet.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);

        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is IAuditable auditable)
            auditable.UpdatedOn = DateTimeOffset.UtcNow;

        if (entity is IVersionable versionable)
            versionable.Version += 1;

        await DbSet.ReplaceOneAsync(
            e => e.Id.Equals(entity.Id),
            entity,
            new ReplaceOptions(),
            cancellationToken);

        return entity;
    }

    // ---------------------------------------------------------------------------
    // Delete
    // ---------------------------------------------------------------------------

    public Task DeleteRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var ids = entities.Select(e => e.Id).ToList();
        return DbSet.DeleteManyAsync(e => ids.Contains(e.Id), cancellationToken);
    }

    public Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => DbSet.DeleteOneAsync(predicate, cancellationToken);

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        => DbSet.DeleteOneAsync(e => e.Id.Equals(entity.Id), cancellationToken);

    public Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default)
        => DbSet.DeleteOneAsync(e => e.Id.Equals(id), cancellationToken);

    // ---------------------------------------------------------------------------
    // Aggregation
    // ---------------------------------------------------------------------------

    public async Task<IReadOnlyList<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<TEntity, TResult> pipeline,
        CancellationToken ct = default)
    {
        var cursor = await DbSet.AggregateAsync(pipeline, cancellationToken: ct);
        return await cursor.ToListAsync(ct);
    }

    public async Task<TResult?> AggregateOneAsync<TResult>(
        PipelineDefinition<TEntity, TResult> pipeline,
        CancellationToken ct = default)
    {
        var cursor = await DbSet.AggregateAsync(pipeline, cancellationToken: ct);
        return await cursor.FirstOrDefaultAsync(ct);
    }

    public Task<IReadOnlyList<TResult>> AggregateAsync<TResult>(
        Func<EmptyPipelineDefinition<TEntity>, PipelineDefinition<TEntity, TResult>> pipelineBuilder,
        CancellationToken ct = default)
        => AggregateAsync(pipelineBuilder(new EmptyPipelineDefinition<TEntity>()), ct);

    public async Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await DbSet.CountDocumentsAsync(predicate, cancellationToken: ct);

    public async Task<IReadOnlyList<GroupResult<TKey>>> GroupAndSumAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<TEntity, long>> sumSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var pipeline = new EmptyPipelineDefinition<TEntity>();

        PipelineDefinition<TEntity, GroupResult<TKey>> typed = predicate is not null
            ? pipeline
                .Match(predicate)
                .Group(
                    keySelector,
                    g => new GroupResult<TKey>(
                        g.Key,
                        g.LongCount(),
                        g.Sum(sumSelector.Compile())))
            : pipeline
                .Group(
                    keySelector,
                    g => new GroupResult<TKey>(
                        g.Key,
                        g.LongCount(),
                        g.Sum(sumSelector.Compile())));

        var cursor = await DbSet.AggregateAsync(typed, cancellationToken: ct);
        return await cursor.ToListAsync(ct);
    }

    public void Dispose() { }
}