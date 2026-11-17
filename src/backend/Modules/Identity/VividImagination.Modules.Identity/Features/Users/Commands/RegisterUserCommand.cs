using MediatR;
using VividImagination.Modules.Identity.Features.Users.Dtos;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Users.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    SafeBrowsingMode? SafeBrowsingMode = null) : IRequest<RegisterUserResult>;
