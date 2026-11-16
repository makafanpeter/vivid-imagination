namespace VividImagination.Shared.Infrastructure.Configurations;

public class JwtSettings:ISettings
{

    public long AccessTokenLifetime { get; set; } = 30;
    public int RefreshTokenLifetime { get; set; } = 3;
}
