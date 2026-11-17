using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile.Services;

public sealed class DynamicBaseAddressHandler(IConfigurationService configService) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var activeBase = new Uri(configService.ActiveBaseUrl);
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = activeBase.Scheme,
                Host = activeBase.Host,
                Port = activeBase.Port
            };
            request.RequestUri = uriBuilder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
