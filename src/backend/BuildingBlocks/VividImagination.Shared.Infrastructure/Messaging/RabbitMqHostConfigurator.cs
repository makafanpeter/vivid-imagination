using System.Security.Authentication;
using MassTransit;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Shared.Infrastructure.Messaging;

public static class RabbitMqHostConfigurator
{
    public static void Configure(
        IRabbitMqBusFactoryConfigurator cfg,
        RabbitMqSettings settings)
    {
        cfg.Host(new Uri(settings.Host), h =>
        {
            h.Username(settings.Username);
            h.Password(settings.Password);
            if (settings.UseSsl)
            {
                h.UseSsl(s => { s.Protocol = SslProtocols.Tls12; });
            }
        });
    }
}