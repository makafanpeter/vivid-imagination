using MediatR;
using Microsoft.AspNetCore.Identity;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Identity.Features.Preferences.Queries;

public class GetSafeBrowsingPreferenceQueryHandler(UserManager<User> userManager)
    : IRequestHandler<GetSafeBrowsingPreferenceQuery, SafeBrowsingPreferenceDto>
{
    public async Task<SafeBrowsingPreferenceDto> Handle(
        GetSafeBrowsingPreferenceQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        return new SafeBrowsingPreferenceDto(user.Id, user.SafeBrowsingMode, user.IsPersonalisationEnabled);
    }
}
