using MediatR;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Social.Features.Votes.Dtos;
using VividImagination.Modules.Social.Persistence;

namespace VividImagination.Modules.Social.Features.Votes.Queries;

public class GetVoteOnPhotoQueryHandler(SocialDbContext db) : IRequestHandler<GetVoteOnPhotoQuery, VoteResult>
{
    public async Task<VoteResult> Handle(GetVoteOnPhotoQuery request, CancellationToken cancellationToken)
    {
        var totals = await db.Votes
            .Where(v => v.PhotoId == request.PhotoId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Up = g.Count(v => v.Value == 1),
                Down = g.Count(v => v.Value == -1)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var up = totals?.Up ?? 0;
        var down = totals?.Down ?? 0;

        return new VoteResult(request.PhotoId, up, down);
    }
}
