using System.Linq.Expressions;
using MongoDB.Driver;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Infrastructure.Mongo;

/// <summary>Typed projection — what the pipeline returns.</summary>
public interface IAggregationRepository<TEntity, in TId>
    where TEntity : class, IAggregate<TId>
{
    /// <summary>Run an arbitrary pipeline and project each result to TResult.</summary>
    Task<IReadOnlyList<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<TEntity, TResult> pipeline,
        CancellationToken ct = default);

    /// <summary>Run a pipeline and return the first result (or null).</summary>
    Task<TResult?> AggregateOneAsync<TResult>(
        PipelineDefinition<TEntity, TResult> pipeline,
        CancellationToken ct = default);

    /// <summary>Convenience: build a pipeline starting from an empty pipeline definition.</summary>
    Task<IReadOnlyList<TResult>> AggregateAsync<TResult>(
        Func<EmptyPipelineDefinition<TEntity>,
             PipelineDefinition<TEntity, TResult>> pipelineBuilder,
        CancellationToken ct = default);

    /// <summary>Count documents matching a filter without a full collection scan.</summary>
    Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>$group by a key selector and $sum an amount selector.</summary>
    Task<IReadOnlyList<GroupResult<TKey>>> GroupAndSumAsync<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<TEntity, long>> sumSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);
}

/// <summary>Shared result DTO used by GroupAndSumAsync.</summary>
public record GroupResult<TKey>(
    TKey Key,
    long Count,
    long Sum);
