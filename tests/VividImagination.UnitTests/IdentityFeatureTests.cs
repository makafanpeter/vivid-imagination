using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using OpenIddict.Abstractions;
using VividImagination.Modules.Identity.Features.Preferences.Commands;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Modules.Identity.Features.Preferences.Queries;
using VividImagination.Modules.Identity.Features.Preferences.Validators;
using VividImagination.Modules.Identity.Features.Users.Commands;
using VividImagination.Modules.Identity.Features.Users.Dtos;
using VividImagination.Modules.Identity.Features.Users.Validators;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Web.Identity;
using Xunit;

namespace VividImagination.UnitTests;

public class IdentityFeatureTests
{
    [Fact]
    public void CurrentWebUser_WhenNotAuthenticated_ShouldReturnNullOrFalse()
    {
        // Arrange
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var currentUser = new CurrentWebUser(contextAccessorMock.Object);

        // Act & Assert
        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.GetUserId().Should().BeNull();
        currentUser.GetUserName().Should().BeNull();
        currentUser.CurrentUser.Should().BeNull();
        currentUser.HasPermission("read").Should().BeFalse();
        currentUser.HasScope("identity-api").Should().BeFalse();
        currentUser.IsInRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void CurrentWebUser_WhenAuthenticated_ShouldExtractClaimsCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(OpenIddictConstants.Claims.Subject, "12345"),
            new(OpenIddictConstants.Claims.Email, "creator@vivid.local"),
            new(OpenIddictConstants.Claims.Name, "Alice Creator"),
            new(OpenIddictConstants.Claims.PreferredUsername, "alice"),
            new(OpenIddictConstants.Claims.Role, "Creator"),
            new("safe_browsing_mode", "HideSensitive"),
            new(Constants.StandardScopes.Permissions, "photo:upload"),
            new(OpenIddictConstants.Claims.Scope, "identity-api media-api feed-api")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext { User = principal };
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var currentUser = new CurrentWebUser(contextAccessorMock.Object);

        // Act & Assert
        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.GetUserId().Should().Be(12345L);
        currentUser.GetUserName().Should().Be("alice");
        currentUser.HasPermission("photo:upload").Should().BeTrue();
        currentUser.HasPermission("non_existent").Should().BeFalse();
        currentUser.HasScope("media-api").Should().BeTrue();
        currentUser.HasScope("admin-api").Should().BeFalse();
        currentUser.IsInRole("Creator").Should().BeTrue();
        currentUser.IsInRole("Admin").Should().BeFalse();
        currentUser.IsOwnerOf("12345").Should().BeTrue();
        currentUser.IsOwnerOf("99999").Should().BeFalse();

        var profile = currentUser.CurrentUser;
        profile.Should().NotBeNull();
        profile!.Sub.Should().Be("12345");
        profile.Email.Should().Be("creator@vivid.local");
        profile.FullName.Should().Be("Alice Creator");
        profile.PreferredUsername.Should().Be("alice");
        profile.SafeBrowsingMode.Should().Be(SafeBrowsingMode.HideSensitive);
        profile.Role.Should().Be("Creator");
        profile.Permissions.Should().Contain("photo:upload");
        profile.Scopes.Should().Contain(["identity-api", "media-api", "feed-api"]);
    }

    [Fact]
    public void CurrentWebUser_WhenSafeBrowsingModeClaimMissing_ShouldDefaultToBlurSensitive()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(OpenIddictConstants.Claims.Subject, "999"),
            new(OpenIddictConstants.Claims.Email, "bob@vivid.local")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var currentUser = new CurrentWebUser(contextAccessorMock.Object);

        // Act & Assert
        currentUser.CurrentUser!.SafeBrowsingMode.Should().Be(SafeBrowsingMode.BlurSensitive);
    }

    [Fact]
    public void UpdateSafeBrowsingPreferenceCommandValidator_ShouldValidateUserIdAndMode()
    {
        // Arrange
        var validator = new UpdateSafeBrowsingPreferenceCommandValidator();

        // Act & Assert
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(0, SafeBrowsingMode.BlurSensitive)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(-1, SafeBrowsingMode.BlurSensitive)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(1, (SafeBrowsingMode)99)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(1, SafeBrowsingMode.HideSensitive)).IsValid.Should().BeTrue();
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(1, SafeBrowsingMode.AlwaysShowSensitive)).IsValid.Should().BeTrue();
        validator.Validate(new UpdateSafeBrowsingPreferenceCommand(1, SafeBrowsingMode.BlurSensitive)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSafeBrowsingPreferenceCommandHandler_WhenUserExists_ShouldUpdatePreference()
    {
        // Arrange
        var user = new User
        {
            Id = 555L,
            UserName = "testuser",
            Email = "test@vivid.local",
            SafeBrowsingMode = SafeBrowsingMode.BlurSensitive
        };

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync("555"))
            .ReturnsAsync(user);

        userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new UpdateSafeBrowsingPreferenceCommandHandler(userManagerMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateSafeBrowsingPreferenceCommand(555L, SafeBrowsingMode.AlwaysShowSensitive),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(555L);
        result.SafeBrowsingMode.Should().Be(SafeBrowsingMode.AlwaysShowSensitive);
        user.SafeBrowsingMode.Should().Be(SafeBrowsingMode.AlwaysShowSensitive);
        user.UpdatedOn.Should().NotBeNull();
        userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateSafeBrowsingPreferenceCommandHandler_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync("999"))
            .ReturnsAsync((User?)null);

        var handler = new UpdateSafeBrowsingPreferenceCommandHandler(userManagerMock.Object);

        // Act & Assert
        var act = () => handler.Handle(
            new UpdateSafeBrowsingPreferenceCommand(999L, SafeBrowsingMode.HideSensitive),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetSafeBrowsingPreferenceQueryHandler_WhenUserExists_ShouldReturnPreference()
    {
        // Arrange
        var user = new User
        {
            Id = 777L,
            UserName = "charlie",
            SafeBrowsingMode = SafeBrowsingMode.HideSensitive
        };

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync("777"))
            .ReturnsAsync(user);

        var handler = new GetSafeBrowsingPreferenceQueryHandler(userManagerMock.Object);

        // Act
        var result = await handler.Handle(new GetSafeBrowsingPreferenceQuery(777L), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(777L);
        result.SafeBrowsingMode.Should().Be(SafeBrowsingMode.HideSensitive);
    }

    [Fact]
    public void RegisterUserCommandValidator_ShouldValidateEmailAndPassword()
    {
        // Arrange
        var validator = new VividImagination.Modules.Identity.Features.Users.Validators.RegisterUserCommandValidator();

        // Act & Assert
        // Invalid email
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "", "Password123!")).IsValid.Should().BeFalse();
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "not-an-email", "Password123!")).IsValid.Should().BeFalse();

        // Weak password (< 8 chars, no digit, no upper, no special)
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "valid@vivid.local", "short1!")).IsValid.Should().BeFalse();
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "valid@vivid.local", "nouppercase1!")).IsValid.Should().BeFalse();
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "valid@vivid.local", "NOLOWERCASE1!")).IsValid.Should().BeFalse();
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "valid@vivid.local", "NoSpecialDigit1")).IsValid.Should().BeFalse();

        // Valid command
        validator.Validate(new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
            "valid@vivid.local", "StrongPassword123!")).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUserCommandHandler_WhenEmailExists_ShouldThrowBadRequestException()
    {
        // Arrange
        var existingUser = new User { Id = 100L, Email = "existing@vivid.local", UserName = "existing@vivid.local" };
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByEmailAsync("existing@vivid.local"))
            .ReturnsAsync(existingUser);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var handler = new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommandHandler(
            userManagerMock.Object, publishEndpointMock.Object);

        // Act & Assert
        var act = () => handler.Handle(
            new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
                "existing@vivid.local", "StrongPassword123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterUserCommandHandler_WhenValid_ShouldCreateUserAndPublishEvent()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByEmailAsync("newuser@vivid.local"))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), "StrongPassword123!"))
            .Callback<User, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var handler = new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommandHandler(
            userManagerMock.Object, publishEndpointMock.Object);

        // Act
        var result = await handler.Handle(
            new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
                "NewUser@vivid.local ", "StrongPassword123!", "Jane", "Doe", SafeBrowsingMode.AlwaysShowSensitive),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@vivid.local");
        result.FullName.Should().Be("Jane Doe");
        result.SafeBrowsingMode.Should().Be(SafeBrowsingMode.AlwaysShowSensitive);

        capturedUser.Should().NotBeNull();
        capturedUser!.UserName.Should().Be("newuser@vivid.local");
        capturedUser.Email.Should().Be("newuser@vivid.local");
        capturedUser.FirstName.Should().Be("Jane");
        capturedUser.LastName.Should().Be("Doe");
        capturedUser.SafeBrowsingMode.Should().Be(SafeBrowsingMode.AlwaysShowSensitive);

        publishEndpointMock.Verify(p => p.Publish(
            It.Is<VividImagination.Modules.Identity.Contracts.Events.UserRegisteredIntegrationEvent>(
                e => e.Email == "newuser@vivid.local" && e.UserName == "newuser@vivid.local"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleSignInCommandHandler_WhenNewUser_ShouldPerformJitSignup()
    {
        // Arrange
        var validatorMock = new Mock<VividImagination.Modules.Identity.Features.Auth.Services.IGoogleTokenValidator>();
        validatorMock.Setup(v => v.ValidateAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VividImagination.Modules.Identity.Features.Auth.Services.GoogleTokenPayload(
                "google-sub-12345",
                "googleuser@gmail.com",
                true,
                "Google",
                "Creator"));

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByLoginAsync("Google", "google-sub-12345"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("googleuser@gmail.com"))
            .ReturnsAsync((User?)null);

        User? capturedJitUser = null;
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedJitUser = u)
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var handler = new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommandHandler(
            validatorMock.Object, userManagerMock.Object, publishEndpointMock.Object);

        // Act
        var result = await handler.Handle(
            new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommand("valid_google_token"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsNewUser.Should().BeTrue();
        result.Email.Should().Be("googleuser@gmail.com");
        result.FullName.Should().Be("Google Creator");

        capturedJitUser.Should().NotBeNull();
        capturedJitUser!.UserName.Should().Be("googleuser@gmail.com");
        capturedJitUser.Email.Should().Be("googleuser@gmail.com");
        capturedJitUser.EmailConfirmed.Should().BeTrue();
        capturedJitUser.SafeBrowsingMode.Should().Be(SafeBrowsingMode.BlurSensitive);

        userManagerMock.Verify(m => m.AddLoginAsync(
            capturedJitUser,
            It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == "google-sub-12345")), Times.Once);

        publishEndpointMock.Verify(p => p.Publish(
            It.Is<VividImagination.Modules.Identity.Contracts.Events.UserRegisteredIntegrationEvent>(
                e => e.Email == "googleuser@gmail.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleSignInCommandHandler_WhenExistingUserMatchesEmail_ShouldLinkGoogleLogin()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 888L,
            Email = "existing@gmail.com",
            UserName = "existing@gmail.com",
            FirstName = "Existing",
            LastName = "User"
        };

        var validatorMock = new Mock<VividImagination.Modules.Identity.Features.Auth.Services.IGoogleTokenValidator>();
        validatorMock.Setup(v => v.ValidateAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VividImagination.Modules.Identity.Features.Auth.Services.GoogleTokenPayload(
                "google-sub-88888",
                "existing@gmail.com",
                true,
                "Existing",
                "User"));

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByLoginAsync("Google", "google-sub-88888"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("existing@gmail.com"))
            .ReturnsAsync(existingUser);

        userManagerMock.Setup(m => m.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var handler = new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommandHandler(
            validatorMock.Object, userManagerMock.Object, publishEndpointMock.Object);

        // Act
        var result = await handler.Handle(
            new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommand("valid_google_token"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsNewUser.Should().BeFalse();
        result.UserId.Should().Be(888L);
        result.Email.Should().Be("existing@gmail.com");

        userManagerMock.Verify(m => m.AddLoginAsync(
            existingUser,
            It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == "google-sub-88888")), Times.Once);

        // User already exists, should NOT publish registration event
        publishEndpointMock.Verify(p => p.Publish(
            It.IsAny<VividImagination.Modules.Identity.Contracts.Events.UserRegisteredIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<RoleManager<IdentityRole<long>>> CreateRoleManagerMock()
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole<long>>>();
        return new Mock<RoleManager<IdentityRole<long>>>(
            roleStoreMock.Object,
            Array.Empty<IRoleValidator<IdentityRole<long>>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!);
    }

    [Fact]
    public async Task RegisterUserCommandHandler_WhenUserRoleDoesNotExist_ShouldThrowSystemErrorException()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByEmailAsync("newuser@vivid.local"))
            .ReturnsAsync((User?)null);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock.Setup(r => r.RoleExistsAsync(SystemRoleNames.User))
            .ReturnsAsync(false);

        var handler = new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommandHandler(
            userManagerMock.Object, publishEndpointMock.Object, roleManagerMock.Object);

        // Act & Assert
        var act = () => handler.Handle(
            new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
                "newuser@vivid.local", "StrongPassword123!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<SystemErrorException>()
            .WithMessage($"*{SystemRoleNames.User}*");
    }

    [Fact]
    public async Task RegisterUserCommandHandler_WhenUserRoleExists_ShouldAssignRole()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByEmailAsync("newuser@vivid.local"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), SystemRoleNames.User))
            .ReturnsAsync(IdentityResult.Success);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock.Setup(r => r.RoleExistsAsync(SystemRoleNames.User))
            .ReturnsAsync(true);

        var handler = new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommandHandler(
            userManagerMock.Object, publishEndpointMock.Object, roleManagerMock.Object);

        // Act
        var result = await handler.Handle(
            new VividImagination.Modules.Identity.Features.Users.Commands.RegisterUserCommand(
                "newuser@vivid.local", "StrongPassword123!"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), SystemRoleNames.User), Times.Once);
    }

    [Fact]
    public async Task GoogleSignInCommandHandler_WhenNewUserAndRoleDoesNotExist_ShouldThrowSystemErrorException()
    {
        // Arrange
        var validatorMock = new Mock<VividImagination.Modules.Identity.Features.Auth.Services.IGoogleTokenValidator>();
        validatorMock.Setup(v => v.ValidateAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VividImagination.Modules.Identity.Features.Auth.Services.GoogleTokenPayload(
                "google-sub-99999",
                "brandnew@gmail.com",
                true,
                "New",
                "User"));

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByLoginAsync("Google", "google-sub-99999"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("brandnew@gmail.com"))
            .ReturnsAsync((User?)null);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock.Setup(r => r.RoleExistsAsync(SystemRoleNames.User))
            .ReturnsAsync(false);

        var handler = new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommandHandler(
            validatorMock.Object, userManagerMock.Object, publishEndpointMock.Object, roleManagerMock.Object);

        // Act & Assert
        var act = () => handler.Handle(
            new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommand("valid_google_token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<SystemErrorException>()
            .WithMessage($"*{SystemRoleNames.User}*");
    }

    [Fact]
    public async Task GoogleSignInCommandHandler_WhenNewUserAndRoleExists_ShouldAssignRole()
    {
        // Arrange
        var validatorMock = new Mock<VividImagination.Modules.Identity.Features.Auth.Services.IGoogleTokenValidator>();
        validatorMock.Setup(v => v.ValidateAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VividImagination.Modules.Identity.Features.Auth.Services.GoogleTokenPayload(
                "google-sub-99999",
                "brandnew@gmail.com",
                true,
                "New",
                "User"));

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManagerMock.Setup(m => m.FindByLoginAsync("Google", "google-sub-99999"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("brandnew@gmail.com"))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), SystemRoleNames.User))
            .ReturnsAsync(IdentityResult.Success);

        var publishEndpointMock = new Mock<MassTransit.IPublishEndpoint>();

        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock.Setup(r => r.RoleExistsAsync(SystemRoleNames.User))
            .ReturnsAsync(true);

        var handler = new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommandHandler(
            validatorMock.Object, userManagerMock.Object, publishEndpointMock.Object, roleManagerMock.Object);

        // Act
        var result = await handler.Handle(
            new VividImagination.Modules.Identity.Features.Auth.Commands.GoogleSignInCommand("valid_google_token"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsNewUser.Should().BeTrue();
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), SystemRoleNames.User), Times.Once);
    }

    [Theory]
    [InlineData("BlurSensitive", SafeBrowsingMode.BlurSensitive)]
    [InlineData("HideSensitive", SafeBrowsingMode.HideSensitive)]
    [InlineData("AlwaysShowSensitive", SafeBrowsingMode.AlwaysShowSensitive)]
    public void RegisterUserRequest_WhenDeserializedFromStringEnum_ShouldSucceed(string modeString, SafeBrowsingMode expectedMode)
    {
        // Arrange
        var json = $$"""
        {
            "email": "test@example.com",
            "password": "Password123!",
            "firstName": "John",
            "lastName": "Doe",
            "safeBrowsingMode": "{{modeString}}"
        }
        """;

        // Act - deserialize using default JsonSerializer options (which relies on [JsonConverter] attribute)
        var request = JsonSerializer.Deserialize<RegisterUserRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        request.Should().NotBeNull();
        request!.Email.Should().Be("test@example.com");
        request.Password.Should().Be("Password123!");
        request.FirstName.Should().Be("John");
        request.LastName.Should().Be("Doe");
        request.SafeBrowsingMode.Should().Be(expectedMode);
    }

    [Fact]
    public void RegisterUserRequest_WhenDeserializedFromNumericEnum_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "email": "test@example.com",
            "password": "Password123!",
            "safeBrowsingMode": 1
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<RegisterUserRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        request.Should().NotBeNull();
        request!.SafeBrowsingMode.Should().Be(SafeBrowsingMode.HideSensitive);
    }

    [Fact]
    public async Task RegisterUserCommandValidator_WhenValid_ShouldPass()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("valid.user@example.com", "SecureP@ssw0rd!", "Jane", "Doe", SafeBrowsingMode.BlurSensitive);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short", "Password must be at least 8 characters long.")]
    [InlineData("nocapital123!", "Password must contain at least one uppercase letter.")]
    [InlineData("NOLOWERCASE123!", "Password must contain at least one lowercase letter.")]
    [InlineData("NoDigitsHere!!", "Password must contain at least one digit.")]
    [InlineData("NoSpecialChar123", "Password must contain at least one special character.")]
    public async Task RegisterUserCommandValidator_WhenPasswordInvalid_ShouldFail(string password, string expectedError)
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("valid@example.com", password);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedError);
    }

    [Fact]
    public async Task RegisterUserCommandValidator_WhenSafeBrowsingModeOutOfRange_ShouldFail()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("valid@example.com", "ValidPass123!", null, null, (SafeBrowsingMode)999);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid safe browsing mode specified.");
    }
}
