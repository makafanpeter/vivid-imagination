namespace VividImagination.Shared.Domain.Entities;

public abstract class BaseEntity<TKey> : IHasKey<TKey>, IAuditable,IVersionable, IEquatable<BaseEntity<TKey>>
{
    /// <summary>
    /// Unique identifier of the entity
    /// </summary>
    public required TKey Id { get; set; }
    
    /// <summary>
    /// Date and time when the entity was created
    /// </summary>
    public virtual DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Date and time when the entity was last updated
    /// </summary>
    public virtual DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// Checks if the entity has a default / unset key value
    /// </summary>
    public virtual bool IsTransientRecord() => HasDefaultKey(this);

    /// <summary>
    /// Gets the actual type of the entity, handling proxy types
    /// </summary>
    protected virtual Type GetUnproxiedType() => GetType();

    private static bool HasDefaultKey(BaseEntity<TKey> entity) => 
        entity != null && EqualityComparer<TKey>.Default.Equals(entity.Id, default);

    public override bool Equals(object? obj) => Equals(obj as BaseEntity<TKey>);

    public virtual bool Equals(BaseEntity<TKey>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!HasDefaultKey(this) && !HasDefaultKey(other) && Equals(Id, other.Id))
        {
            var otherType = other.GetUnproxiedType();
            var thisType = GetUnproxiedType();
            return thisType.IsAssignableFrom(otherType) || 
                   otherType.IsAssignableFrom(thisType);
        }

        return false;
    }

    public override int GetHashCode() => 
        HasDefaultKey(this) ? base.GetHashCode() : Id?.GetHashCode() ?? 0;

    public static bool operator ==(BaseEntity<TKey>? x, BaseEntity<TKey>? y) => 
        x?.Equals(y) ?? y is null;

    public static bool operator !=(BaseEntity<TKey>? x, BaseEntity<TKey>? y) => !(x == y);

    /// <summary>
    ///  The version of the entity
    /// </summary>
    public long Version { get; set; }
}