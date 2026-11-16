using MassTransit;

namespace VividImagination.Shared.Infrastructure.Messaging;

public interface IMessagingModule
{
    void RegisterConsumers(IRegistrationConfigurator cfg);
}