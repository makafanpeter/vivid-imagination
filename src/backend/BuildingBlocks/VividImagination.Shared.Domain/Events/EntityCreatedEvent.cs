using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Domain.Events;

public class EntityCreatedEvent<T> : IDomainEvent
    where T : BaseEntity<Guid>
{
    public EntityCreatedEvent(T entity, DateTime eventDateTime)
    {
        Entity = entity;
        EventDateTime = eventDateTime;
    }

    public T Entity { get; }

    public DateTime EventDateTime { get; }
}