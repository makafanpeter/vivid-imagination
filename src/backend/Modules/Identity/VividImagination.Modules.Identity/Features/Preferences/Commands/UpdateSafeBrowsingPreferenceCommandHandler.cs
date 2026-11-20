using MediatR;
using Microsoft.AspNetCore.Identity;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Identity.Features.Preferences.Commands;

public class UpdateSafeBrowsingPreferenceCommandHandler(UserManager<User> userManager)
    : IRequestHandler<UpdateSafeBrowsingPreferenceCommand, SafeBrowsingPreferenceDto>
{
    public async Task<SafeBrowsingPreferenceDto> Handle(
        UpdateSafeBrowsingPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        user.SafeBrowsingMode = request.Mode;
        if (request.IsPersonalisationEnabled.HasValue)
        {
            user.IsPersonalisationEnabled = request.IsPersonalisationEnabled.Value;
        }
        user.UpdatedOn = DateTimeOffset.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Failed to update user preferences: {errors}");
        }

        return new SafeBrowsingPreferenceDto(user.Id, user.SafeBrowsingMode, user.IsPersonalisationEnabled);
    }
}
