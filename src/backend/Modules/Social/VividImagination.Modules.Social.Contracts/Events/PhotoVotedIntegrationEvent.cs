using VividImagination.Shared.Contract.Events;

namespace VividImagination.Modules.Social.Contracts.Events;

public record PhotoVotedIntegrationEvent(
    Guid PhotoId,
    long UserId,
    int VoteDelta,
    int TotalUpvotes,
    int TotalDownvotes,
    DateTime VotedAt) : IntegrationEvent(PhotoId, VotedAt);
