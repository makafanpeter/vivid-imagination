using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Infrastructure.Mongo;

public interface IMongoRepository<TEntity, in TId> : IRepository<TEntity, TId>
    where TEntity : class, IAggregate<TId>
{
}
