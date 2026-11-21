using MediatR;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Preferences.Commands;

public sealed record UpdateSafeBrowsingPreferenceCommand(long UserId, SafeBrowsingMode Mode, bool? IsPersonalisationEnabled = null)
    : IRequest<SafeBrowsingPreferenceDto>;
