using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Social.Persistence.Entities;

public class PhotoVote: BaseEntity<Guid>
{
    public Guid PhotoId { get; set; }
    public long UserId { get; set; }
    public int Value { get; set; } // +1 for Upvote, -1 for Downvote
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}