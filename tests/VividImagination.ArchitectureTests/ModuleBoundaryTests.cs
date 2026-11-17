using FluentAssertions;
using NetArchTest.Rules;
using VividImagination.Modules.Feed;
using VividImagination.Modules.Identity;
using VividImagination.Modules.Media;
using VividImagination.Modules.Moderation;
using VividImagination.Modules.Social;
using Xunit;

namespace VividImagination.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string[] InternalImplementationNamespaces =
    [
        "VividImagination.Modules.Identity.Controllers",
        "VividImagination.Modules.Identity.Entities",
        "VividImagination.Modules.Identity.Persistence",
        "VividImagination.Modules.Media.Controllers",
        "VividImagination.Modules.Media.Entities",
        "VividImagination.Modules.Media.Persistence",
        "VividImagination.Modules.Media.Features",
        "VividImagination.Modules.Media.Services",
        "VividImagination.Modules.Social.Controllers",
        "VividImagination.Modules.Social.Persistence",
        "VividImagination.Modules.Social.Features",
        "VividImagination.Modules.Moderation.Controllers",
        "VividImagination.Modules.Moderation.Persistence",
        "VividImagination.Modules.Moderation.Features",
        "VividImagination.Modules.Moderation.Services",
        "VividImagination.Modules.Feed.Controllers",
        "VividImagination.Modules.Feed.Persistence",
        "VividImagination.Modules.Feed.Ranking"
    ];

    [Fact]
    public void Moderation_Module_Should_Not_Have_Dependencies_On_Other_Module_Implementations()
    {
        var forbidden = InternalImplementationNamespaces
            .Where(ns => !ns.StartsWith("VividImagination.Modules.Moderation"))
            .ToArray();

        var result = Types.InAssembly(typeof(ModerationModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"ModerationModule must not depend on other module implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Social_Module_Should_Not_Have_Dependencies_On_Other_Module_Implementations()
    {
        var forbidden = InternalImplementationNamespaces
            .Where(ns => !ns.StartsWith("VividImagination.Modules.Social"))
            .ToArray();

        var result = Types.InAssembly(typeof(SocialModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"SocialModule must not depend on other module implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Identity_Module_Should_Not_Have_Dependencies_On_Other_Module_Implementations()
    {
        var forbidden = InternalImplementationNamespaces
            .Where(ns => !ns.StartsWith("VividImagination.Modules.Identity"))
            .ToArray();

        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"IdentityModule must not depend on other module implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Feed_Module_Should_Not_Have_Dependencies_On_Other_Module_Implementations()
    {
        var forbidden = InternalImplementationNamespaces
            .Where(ns => !ns.StartsWith("VividImagination.Modules.Feed"))
            .ToArray();

        var result = Types.InAssembly(typeof(FeedModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"FeedModule must not depend on other module implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Media_Module_Should_Not_Have_Dependencies_On_Other_Module_Implementations()
    {
        var forbidden = InternalImplementationNamespaces
            .Where(ns => !ns.StartsWith("VividImagination.Modules.Media"))
            .ToArray();

        var result = Types.InAssembly(typeof(MediaModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"MediaModule must not depend on other module implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Modules_Should_Not_Reference_Other_Module_Implementation_Assemblies()
    {
        var moduleAssemblies = new[]
        {
            typeof(ModerationModule).Assembly,
            typeof(SocialModule).Assembly,
            typeof(FeedModule).Assembly,
            typeof(MediaModule).Assembly,
            typeof(IdentityModule).Assembly
        };

        foreach (var assembly in moduleAssemblies)
        {
            var otherModuleAssemblyNames = moduleAssemblies
                .Where(a => a != assembly)
                .Select(a => a.GetName().Name!)
                .ToArray();

            var referencedAssemblies = assembly.GetReferencedAssemblies()
                .Select(a => a.Name);

            referencedAssemblies.Should().NotContain(otherModuleAssemblyNames,
                $"{assembly.GetName().Name} must not reference any other module implementation assembly directly. Inter-module communication must use *.Contracts!");
        }
    }

    [Fact]
    public void Amazon_Rekognition_Should_Not_Leak_Outside_Moderation_Module()
    {
        var nonModerationAssemblies = new[]
        {
            typeof(IdentityModule).Assembly,
            typeof(MediaModule).Assembly,
            typeof(SocialModule).Assembly,
            typeof(FeedModule).Assembly
        };

        foreach (var assembly in nonModerationAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Amazon.Rekognition")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Amazon Rekognition SDK leaked into {assembly.GetName().Name}! It must remain strictly isolated inside ModerationModule.");
        }
    }
}
