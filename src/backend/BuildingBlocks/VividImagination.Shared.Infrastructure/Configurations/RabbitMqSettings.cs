namespace VividImagination.Shared.Infrastructure.Configurations;

public class RabbitMqSettings
{
    public bool UseSsl { get; set; } = false;
    public string Host { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; }= default!;
}