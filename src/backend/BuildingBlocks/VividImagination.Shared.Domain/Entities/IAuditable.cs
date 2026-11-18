namespace VividImagination.Shared.Domain.Entities;

public interface IAuditable {

    DateTimeOffset CreatedOn { get; set; }

    DateTimeOffset? UpdatedOn { get; set; }
}