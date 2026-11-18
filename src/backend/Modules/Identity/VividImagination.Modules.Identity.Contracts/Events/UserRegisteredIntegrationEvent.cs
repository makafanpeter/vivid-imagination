using VividImagination.Shared.Contract.Events;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Contracts.Events;

public record UserRegisteredIntegrationEvent(
    long UserId,
    string Email,
    string UserName,
    string? FirstName,
    string? LastName,
    SafeBrowsingMode SafeBrowsingMode,
    DateTime CreatedAt) : IntegrationEvent(Guid.NewGuid(), CreatedAt);
