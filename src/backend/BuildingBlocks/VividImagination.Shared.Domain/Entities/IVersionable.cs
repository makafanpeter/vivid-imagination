namespace VividImagination.Shared.Domain.Entities;

/// <summary>
/// Represents an entity that has a version number for optimistic concurrency control.
/// </summary>
public interface IVersionable
{
    /// <summary>
    /// Gets or sets the version number of the entity.
    /// </summary>
    long Version { get; set; }
}
