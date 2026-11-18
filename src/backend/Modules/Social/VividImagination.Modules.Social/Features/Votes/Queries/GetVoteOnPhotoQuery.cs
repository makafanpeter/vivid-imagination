using MediatR;
using VividImagination.Modules.Social.Features.Votes.Dtos;

namespace VividImagination.Modules.Social.Features.Votes.Queries;

public record GetVoteOnPhotoQuery(Guid PhotoId) : IRequest<VoteResult>;
