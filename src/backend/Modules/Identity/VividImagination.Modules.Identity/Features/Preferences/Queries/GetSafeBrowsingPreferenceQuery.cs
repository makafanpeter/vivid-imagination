using MediatR;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;

namespace VividImagination.Modules.Identity.Features.Preferences.Queries;

public sealed record GetSafeBrowsingPreferenceQuery(long UserId) : IRequest<SafeBrowsingPreferenceDto>;
