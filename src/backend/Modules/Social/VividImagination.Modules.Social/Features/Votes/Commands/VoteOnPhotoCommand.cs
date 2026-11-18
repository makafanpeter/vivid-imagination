using MediatR;
using VividImagination.Modules.Social.Features.Votes.Dtos;

namespace VividImagination.Modules.Social.Features.Votes.Commands;

public record VoteOnPhotoCommand(Guid PhotoId, int Value) : IRequest<VoteResult>;
