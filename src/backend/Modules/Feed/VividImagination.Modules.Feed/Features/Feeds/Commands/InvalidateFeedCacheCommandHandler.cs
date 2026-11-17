using MediatR;

namespace VividImagination.Modules.Feed.Features.Feeds.Commands;

public class InvalidateFeedCacheCommandHandler : IRequestHandler<InvalidateFeedCacheCommand, Unit>
{
    public Task<Unit> Handle(InvalidateFeedCacheCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Unit.Value);
    }
}
