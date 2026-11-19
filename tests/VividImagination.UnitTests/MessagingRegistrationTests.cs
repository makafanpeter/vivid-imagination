using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using VividImagination.Modules.Media;
using VividImagination.Modules.Moderation;
using VividImagination.Shared.Infrastructure.Messaging;
using VividImagination.Shared.Infrastructure.Modules;
using Xunit;

namespace VividImagination.UnitTests;

public class MessagingRegistrationTests
{
    [Fact]
    public void InitializedModuleInstances_Should_Be_Tracked_And_Retrievable_Without_BuildServiceProvider()
    {
        AppModuleExtensions.Reset();

        var mediaModule = new MediaModule();
        var moderationModule = new ModerationModule();

        var services = new ServiceCollection();
        
        // Simulate module initialization tracking
        AppModuleExtensions.InitializedModules.Add(typeof(MediaModule));
        AppModuleExtensions.InitializedModules.Add(typeof(ModerationModule));
        services.AddSingleton(typeof(MediaModule), mediaModule);
        services.AddSingleton(typeof(ModerationModule), moderationModule);

        // Verify that instances can be retrieved from service descriptors directly without BuildServiceProvider
        var discoveredModules = services
            .Where(d => d.ImplementationInstance is IMessagingModule)
            .Select(d => (IMessagingModule)d.ImplementationInstance!)
            .ToList();

        discoveredModules.Should().HaveCount(2);
        discoveredModules.Select(m => ((IAppModule)m).ModuleName).Should().Contain(["Media", "Moderation"]);

        services.AddMassTransit(x =>
        {
            foreach (var module in discoveredModules)
            {
                module.RegisterConsumers(x);
            }
        });

        // MassTransit registers consumers into IServiceCollection
        services.Should().Contain(d => d.ServiceType == typeof(VividImagination.Modules.Media.Features.Photos.Consumers.ValidatePhotoConsumer));
        services.Should().Contain(d => d.ServiceType == typeof(VividImagination.Modules.Moderation.Features.Reviews.Consumers.PhotoUploadedConsumer));
    }

    [Fact]
    public void GetInitializedModuleInstances_Should_Return_Empty_After_Reset()
    {
        AppModuleExtensions.Reset();
        AppModuleExtensions.GetInitializedModuleInstances().Should().BeEmpty();
    }
}
