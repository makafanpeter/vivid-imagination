namespace VividImagination.Shared.Infrastructure.Configurations;

public class HttpSettings
{
    public string ForwardedHttpHeader { get; set; } = string.Empty;
    public bool UseHttpClusterHttps { get; set; }
    public bool UseHttpXForwardedProto { get; set; }
    public bool UseInstanceProfile { get; set; }
}