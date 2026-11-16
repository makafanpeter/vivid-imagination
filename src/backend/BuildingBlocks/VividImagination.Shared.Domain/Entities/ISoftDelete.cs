namespace VividImagination.Shared.Domain.Entities;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}