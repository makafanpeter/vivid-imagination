namespace VividImagination.Shared.Infrastructure.Configurations;

public class EventBusSettings
{
    public SQSSettings SQS { get; set; } = new();
    public RabbitMqSettings RabbitMQ { get; set; } = new();
}