namespace VividImagination.Shared.Infrastructure.Configurations;

public class OpenIdConnectSettings
{
    public string Authority { get; set; }
    public string[] Audiences { get; set; }
    public string IssuerUri { get; set; }
    public string DefaultClientSecret { get; set; }
    public bool EnableValidation { get; set; }
}