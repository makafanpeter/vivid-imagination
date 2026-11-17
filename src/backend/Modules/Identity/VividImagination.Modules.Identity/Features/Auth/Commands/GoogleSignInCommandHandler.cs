using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VividImagination.Modules.Identity.Contracts.Events;
using VividImagination.Modules.Identity.Features.Auth.Dtos;
using VividImagination.Modules.Identity.Features.Auth.Services;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.IdsGenerator;

namespace VividImagination.Modules.Identity.Features.Auth.Commands;

public class GoogleSignInCommandHandler(
    IGoogleTokenValidator tokenValidator,
    UserManager<User> userManager,
    IPublishEndpoint publishEndpoint,
    RoleManager<IdentityRole<long>>? roleManager = null)
    : IRequestHandler<GoogleSignInCommand, GoogleSignInResult>
{
    public async Task<GoogleSignInResult> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new BadRequestException("Google ID token is required.", "INVALID_TOKEN");
        }

        var payload = await tokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (payload == null || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new BadRequestException("Invalid or expired Google token.", "INVALID_GOOGLE_TOKEN");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var isNewUser = false;

        // 1. Check if user is already linked with this Google login
        var user = await userManager.FindByLoginAsync("Google", payload.Subject);

        // 2. If not found by login, check if user exists with the same email
        if (user == null)
        {
            user = await userManager.FindByEmailAsync(normalizedEmail);

            if (user != null)
            {
                // Link Google login to existing account
                await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
            }
        }

        // 3. If user does not exist, perform Just-In-Time (JIT) Signup
        if (user == null)
        {
            isNewUser = true;

            // Ensure 'User' role exists
            if (roleManager != null && !await roleManager.RoleExistsAsync(SystemRoleNames.User))
            {
                await roleManager.CreateAsync(new IdentityRole<long>
                {
                    Id = SnowFlakIdGenerator.NewId(),
                    Name = SystemRoleNames.User,
                    NormalizedName = SystemRoleNames.User.ToUpperInvariant()
                });
            }

            user = new User
            {
                Id = SnowFlakIdGenerator.NewId(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FirstName = payload.GivenName?.Trim(),
                LastName = payload.FamilyName?.Trim(),
                SafeBrowsingMode = request.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive,
                CreatedOn = DateTimeOffset.UtcNow,
                EmailConfirmed = payload.EmailVerified
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to provision Google account: {errors}", "PROVISIONING_FAILED");
            }

            // Link Google external login
            await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));

            // Assign default role
            if (roleManager != null)
            {
                await userManager.AddToRoleAsync(user, SystemRoleNames.User);
            }

            // Publish integration event
            await publishEndpoint.Publish(new UserRegisteredIntegrationEvent(
                user.Id,
                user.Email,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.SafeBrowsingMode,
                user.CreatedOn.UtcDateTime), cancellationToken);
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return new GoogleSignInResult(
            user.Id,
            user.Email ?? normalizedEmail,
            string.IsNullOrEmpty(fullName) ? null : fullName,
            user.SafeBrowsingMode,
            isNewUser);
    }
}
