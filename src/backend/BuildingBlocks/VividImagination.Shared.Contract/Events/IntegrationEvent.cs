namespace VividImagination.Shared.Contract.Events;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredOn);
