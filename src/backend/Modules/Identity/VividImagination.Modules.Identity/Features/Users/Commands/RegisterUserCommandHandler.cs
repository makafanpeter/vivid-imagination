using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Identity.Contracts.Events;
using VividImagination.Modules.Identity.Features.Users.Dtos;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.IdsGenerator;

namespace VividImagination.Modules.Identity.Features.Users.Commands;

public class RegisterUserCommandHandler(
    UserManager<User> userManager,
    IPublishEndpoint publishEndpoint,
    ILogger<RegisterUserCommandHandler> logger,
    RoleManager<IdentityRole<long>>? roleManager = null)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Verify email uniqueness
        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            throw new BadRequestException("An account with this email address already exists.", "EMAIL_ALREADY_EXISTS");
        }

        // 2. Validate default 'User' role exists if roleManager is configured
        if (roleManager != null && !await roleManager.RoleExistsAsync(SystemRoleNames.User))
        {
            logger.LogError("Default role '{RoleName}' does not exist. Ensure database seeding has completed.", SystemRoleNames.User);   
            throw new SystemErrorException();
        }

        // 3. Create user entity with UserName = normalized email
        var user = new User
        {
            Id = SnowFlakIdGenerator.NewId(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            SafeBrowsingMode = request.SafeBrowsingMode ?? SafeBrowsingMode.BlurSensitive,
            CreatedOn = DateTimeOffset.UtcNow,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Failed to create user account: {errors}", "REGISTRATION_FAILED");
        }

        // 4. Assign default role
        if (roleManager != null)
        {
            await userManager.AddToRoleAsync(user, SystemRoleNames.User);
        }

        // 5. Publish integration event over MassTransit
        await publishEndpoint.Publish(new UserRegisteredIntegrationEvent(
            user.Id,
            user.Email,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.SafeBrowsingMode,
            user.CreatedOn.UtcDateTime), cancellationToken);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return new RegisterUserResult(
            user.Id,
            user.Email,
            string.IsNullOrEmpty(fullName) ? null : fullName,
            user.SafeBrowsingMode,
            user.CreatedOn);
    }
}
