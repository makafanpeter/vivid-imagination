namespace VividImagination.Shared.Infrastructure.Configurations;

public class CORS
{
    public bool AllowAnyOrigin { get; set; }

    public string[]? AllowedOrigins { get; set; }
}