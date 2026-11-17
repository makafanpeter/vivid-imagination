using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.Web.Helpers;

public class WebHelper : IWebHelper
{
    #region Fields

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly Uri DummyBase = new("http://localhost");

    private readonly ApplicationSettings _appSettings;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Ctor

    public WebHelper(IOptions<ApplicationSettings> appSettings,
        IHostApplicationLifetime hostApplicationLifetime,
        IHttpContextAccessor httpContextAccessor)
    {
        _appSettings = appSettings.Value;
        _hostApplicationLifetime = hostApplicationLifetime;
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Utilities

    private HttpContext? Context => _httpContextAccessor.HttpContext;

    /// <summary>Check whether current HTTP request is available</summary>
    protected virtual bool IsRequestAvailable()
    {
        try
        {
            return Context?.Request != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Is IP address specified (not null and not the IPv6 loopback)</summary>
    protected virtual bool IsIpAddressSet(IPAddress address) =>
        address != null && !address.Equals(IPAddress.IPv6Loopback);

    /// <summary>Parses an absolute http(s) URL or a local path. Throws on anything else.</summary>
    private static (Uri Uri, bool IsLocal) ParseUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            return (absolute, false);

        // Local path: a single leading "/" (rejects "//host" and "/\host")
        if (url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\"))
            return (new Uri(DummyBase, url), true);

        throw new ArgumentException("URL must be an absolute http(s) URL or a local path.", nameof(url));
    }

    private static string BuildUrl(Uri uri, bool isLocal, QueryBuilder query) =>
        $"{(isLocal ? uri.AbsolutePath : uri.GetLeftPart(UriPartial.Path))}{query.ToQueryString()}{uri.Fragment}";

    #endregion

    #region Methods

    public virtual string GetUrlReferrer()
    {
        if (!IsRequestAvailable())
            return string.Empty;

        // The referrer header can be missing
        return Context!.Request.Headers[HeaderNames.Referer].ToString();
    }

    /// <summary>
    /// Gets the client IP address.
    /// By default this is the connection's remote address. Configure UseForwardedHeaders with
    /// KnownProxies/KnownNetworks so that address is already corrected for your proxy.
    /// A forwarded header is read only when ForwardedHttpHeader is set explicitly in the settings.
    /// </summary>
    public virtual string GetCurrentIpAddress()
    {
        if (!IsRequestAvailable())
            return string.Empty;

        try
        {
            string? result = null;

            var headerName = _appSettings.Http.ForwardedHttpHeader;
            if (!string.IsNullOrEmpty(headerName))
            {
                var forwarded = Context!.Request.Headers[headerName];
                if (!StringValues.IsNullOrEmpty(forwarded))
                    result = forwarded.ToString().Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(result))
                result = Context!.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            // Plain IPv4/IPv6 address first, then "ip:port" and "[ipv6]:port"
            if (IPAddress.TryParse(result, out var ip))
                return Normalize(ip);

            if (IPEndPoint.TryParse(result, out var endpoint))
                return Normalize(endpoint.Address);

            return string.Empty;   // not an IP address: don't store arbitrary header text
        }
        catch
        {
            return string.Empty;
        }

        static string Normalize(IPAddress ip) =>
            ip.Equals(IPAddress.IPv6Loopback) ? IPAddress.Loopback.ToString() : ip.ToString();
    }

    public virtual string GetThisPageUrl(bool includeQueryString, bool? useSsl = null, bool lowercaseUrl = false)
    {
        if (!IsRequestAvailable())
            return string.Empty;

        var request = Context!.Request;
        var pageUrl = request.Path.ToString();

        if (includeQueryString)
            pageUrl += request.QueryString;

        return lowercaseUrl ? pageUrl.ToLowerInvariant() : pageUrl;
    }

    public virtual bool IsCurrentConnectionSecured()
    {
        if (!IsRequestAvailable())
            return false;

        var request = Context!.Request;

        if (_appSettings.Http.UseHttpClusterHttps)
            return request.Headers[HttpDefaults.HttpClusterHttpsHeader].ToString()
                .Equals("on", StringComparison.OrdinalIgnoreCase);

        if (_appSettings.Http.UseHttpXForwardedProto)
            return request.Headers[HttpDefaults.HttpXForwardedProtoHeader].ToString()
                .Equals("https", StringComparison.OrdinalIgnoreCase);

        return request.IsHttps;
    }

    /// <summary>True if the request targets a static resource file.</summary>
    public virtual bool IsStaticResource()
    {
        if (!IsRequestAvailable())
            return false;

        return ContentTypeProvider.TryGetContentType(Context!.Request.Path.ToString(), out _);
    }

    public virtual string ModifyQueryString(string url, string key, params string[] values)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (string.IsNullOrEmpty(key))
            return url;

        var (uri, isLocal) = ParseUrl(url);

        var queryParameters = QueryHelpers.ParseQuery(uri.Query);
        queryParameters[key] = string.Join(",", values);

        var queryBuilder = new QueryBuilder(queryParameters
            .ToDictionary(p => p.Key, p => p.Value.FirstOrDefault() ?? string.Empty));

        return BuildUrl(uri, isLocal, queryBuilder);
    }

    public virtual string RemoveQueryString(string url, string key, string value = null)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (string.IsNullOrEmpty(key))
            return url;

        var (uri, isLocal) = ParseUrl(url);

        var queryParameters = QueryHelpers.ParseQuery(uri.Query)
            .SelectMany(p => p.Value, (p, v) => new KeyValuePair<string, string>(p.Key, v))
            .ToList();

        if (!string.IsNullOrEmpty(value))
        {
            queryParameters.RemoveAll(p =>
                p.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase) &&
                p.Value.Equals(value, StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            queryParameters.RemoveAll(p => p.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        return BuildUrl(uri, isLocal, new QueryBuilder(queryParameters));
    }

    public virtual T QueryString<T>(string name)
    {
        if (!IsRequestAvailable())
            return default;

        var value = Context!.Request.Query[name];
        return StringValues.IsNullOrEmpty(value) ? default : CommonHelper.To<T>(value.ToString());
    }

    public virtual void RestartAppDomain() => _hostApplicationLifetime.StopApplication();

    public virtual bool IsRequestBeingRedirected
    {
        get
        {
            if (!IsRequestAvailable())
                return false;

            var status = Context!.Response.StatusCode;
            return status is StatusCodes.Status301MovedPermanently or StatusCodes.Status302Found;
        }
    }

    public virtual bool IsPostBeingDone
    {
        get
        {
            if (!IsRequestAvailable())
                return false;

            var item = Context!.Items[HttpDefaults.IsPostBeingDoneRequestItem];
            return item != null && Convert.ToBoolean(item);
        }
        set
        {
            if (IsRequestAvailable())
                Context!.Items[HttpDefaults.IsPostBeingDoneRequestItem] = value;
        }
    }

    public virtual string GetCurrentRequestProtocol() =>
        IsCurrentConnectionSecured() ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

    public virtual bool IsLocalRequest(HttpRequest req)
    {
        var connection = req.HttpContext.Connection;
        if (IsIpAddressSet(connection.RemoteIpAddress))
        {
            return IsIpAddressSet(connection.LocalIpAddress)
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                : IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        return true;
    }

    public virtual string GetRawUrl(HttpRequest request)
    {
        // The raw target is not URL-decoded
        var rawUrl = request.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;

        return string.IsNullOrEmpty(rawUrl)
            ? $"{request.PathBase}{request.Path}{request.QueryString}"
            : rawUrl;
    }

    public virtual bool IsAjaxRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    public string GetUserAgent()
    {
        if (!IsRequestAvailable())
            return string.Empty;

        return Context!.Request.Headers.UserAgent.ToString();
    }

    #endregion
}

public static partial class HttpDefaults
{
    public static string DefaultHttpClient => "default";
    public static string IsPostBeingDoneRequestItem => "vivid.IsPOSTBeingDone";
    public static string HttpClusterHttpsHeader => "HTTP_CLUSTER_HTTPS";
    public static string HttpXForwardedProtoHeader => "X-Forwarded-Proto";
    public static string XForwardedForHeader => "X-FORWARDED-FOR";
}