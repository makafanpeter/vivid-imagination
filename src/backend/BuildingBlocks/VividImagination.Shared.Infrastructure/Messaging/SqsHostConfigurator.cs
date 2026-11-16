using Amazon.Runtime;
using MassTransit;
using Microsoft.Extensions.Hosting;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Messaging;

public static class SqsHostConfigurator
{
    public static void Configure(
        IAmazonSqsBusFactoryConfigurator cfg,
        SQSSettings settings,
        IHostEnvironment environment)
    {
        cfg.Host(settings.Region, h =>
        {
            h.Credentials(settings.UseInstanceProfile
                ? new InstanceProfileAWSCredentials()
                : new EnvironmentVariablesAWSCredentials());
            h.Scope(environment.EnvironmentName, true);
            h.EnableScopedTopics();
        });
    }
}