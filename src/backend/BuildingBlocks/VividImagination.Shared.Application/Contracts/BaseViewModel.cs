using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Application.Contracts;

public abstract class BaseViewModel<TKey> : IHasKey<TKey>, IVersionable {

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
    
    public required  TKey Id { get; set; }
    public required long Version { get; set; }
}