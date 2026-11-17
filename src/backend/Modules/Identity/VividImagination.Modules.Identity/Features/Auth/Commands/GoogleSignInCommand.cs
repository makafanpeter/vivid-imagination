using MediatR;
using VividImagination.Modules.Identity.Features.Auth.Dtos;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Auth.Commands;

public sealed record GoogleSignInCommand(
    string IdToken,
    SafeBrowsingMode? SafeBrowsingMode = null) : IRequest<GoogleSignInResult>;
