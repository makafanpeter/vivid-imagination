namespace VividImagination.Shared.Domain.Entities;

public interface IAggregate 
{
  
    long Version { get; set; }
}

public interface IAggregate<TKey> : IAggregate, IHasKey<TKey>
{
    /// <summary>
    /// The unique identifier for the aggregate
    /// </summary>
    new TKey Id { get; } 
}
