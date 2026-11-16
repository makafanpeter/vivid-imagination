using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Social.Contracts.Events;
using VividImagination.Modules.Social.Features.Votes.Dtos;
using VividImagination.Modules.Social.Persistence;
using VividImagination.Modules.Social.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Social.Features.Votes.Commands;

public class VoteOnPhotoCommandHandler(
    SocialDbContext db,
    IPublishEndpoint publisher,
    ICurrentUser currentUser)
    : IRequestHandler<VoteOnPhotoCommand, VoteResult>
{
    public async Task<VoteResult> Handle(VoteOnPhotoCommand request, CancellationToken ct)
    {
        if (currentUser.CurrentUser == null)
        {
            throw new UnAuthorizedException("You are not authorized to vote on photo");
        }

        var userId = currentUser.GetUserId() ?? throw new UnAuthorizedException("You are not authorized to vote on photo");
        var vote = await db.Votes.FirstOrDefaultAsync(
            v => v.PhotoId == request.PhotoId && v.UserId == userId, ct);

        var oldValue = vote?.Value ?? 0;

        if (vote is null)
        {
            db.Votes.Add(new PhotoVote
            {
                Id = Guid.NewGuid(),
                PhotoId = request.PhotoId,
                UserId = userId,
                Value = request.Value,
                VotedAt = DateTime.UtcNow
            });
        }
        else
        {
            vote.Value = request.Value;
            vote.VotedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // One round trip instead of two counts
        var totals = await db.Votes
            .Where(v => v.PhotoId == request.PhotoId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Up = g.Count(v => v.Value == 1),
                Down = g.Count(v => v.Value == -1)
            })
            .FirstOrDefaultAsync(ct);

        var up = totals?.Up ?? 0;
        var down = totals?.Down ?? 0;

        await publisher.Publish(new PhotoVotedIntegrationEvent(
            request.PhotoId,
            userId,
            request.Value - oldValue,
            up,
            down,
            DateTime.UtcNow), ct);

        return new VoteResult(request.PhotoId, up, down);
    }
}
