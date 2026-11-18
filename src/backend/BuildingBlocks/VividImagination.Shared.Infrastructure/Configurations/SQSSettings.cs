namespace VividImagination.Shared.Infrastructure.Configurations;

public class SQSSettings
{
    public string Region { get; set; } = default!;
    public bool UseInstanceProfile { get; set; } = false;
}