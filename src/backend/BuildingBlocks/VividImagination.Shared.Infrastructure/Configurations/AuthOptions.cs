using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Shared.Infrastructure.Configurations;

public class OpenIdConnectSettings
{
    public string Authority { get; set; }
    public string[] Audiences { get; set; }
    public string? Audience  { get; set; }
    public string DefaultClientId { get; set; } = $"{AppConstants.AppName}-client";
    public string IssuerUri { get; set; }
    public string DefaultClientSecret { get; set; }
    public bool EnableValidation { get; set; }
}