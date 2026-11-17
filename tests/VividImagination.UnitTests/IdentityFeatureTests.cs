using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using OpenIddict.Abstractions;
using VividImagination.Modules.Identity.Features.Preferences.Commands;
using VividImagination.Modules.Identity.Features.Preferences.Dtos;
using VividImagination.Modules.Identity.Features.Preferences.Queries;
using VividImagination.Modules.Identity.Features.Preferences.Validators;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
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
}
