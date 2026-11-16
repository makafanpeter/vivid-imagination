namespace VividImagination.Shared.Infrastructure.Configurations;

public class SeoSettings : ISettings
{
    /// <summary>
    /// Base URL for the public customer website (e.g., "https://localchamp.ng" or "http://localhost:3000").
    /// If null or empty, falls back to GeneralSettings.DefaultClientUrl.
    /// </summary>
    public string? BaseWebUrl { get; set; }

    /// <summary>
    /// Brand or site name displayed in page titles and structured data.
    /// If null or empty, falls back to GeneralSettings.AppName.
    /// </summary>
    public string? SiteName { get; set; }

    /// <summary>
    /// Default ISO two-letter country code for geographic and SEO queries (e.g., "NG").
    /// </summary>
    public string DefaultCountryCode { get; set; } = "NG";

    /// <summary>
    /// Default currency code for pricing and schema metadata (e.g., "NGN").
    /// </summary>
    public string DefaultCurrencyCode { get; set; } = "NGN";

    /// <summary>
    /// Default currency symbol for pricing displays (e.g., "₦").
    /// </summary>
    public string DefaultCurrencySymbol { get; set; } = "₦";

    /// <summary>
    /// Minimum number of active verified providers required to index a programmatic page.
    /// If providers count is below this threshold, the page renders "noindex, follow" and is excluded from sitemaps.
    /// </summary>
    public int QualityGateMinVerifiedProviders { get; set; } = 2;

    /// <summary>
    /// URL route segment for category hub pages (e.g., "c" in "/c/{category}/{city}").
    /// </summary>
    public string CategoryPathSegment { get; set; } = "c";

    /// <summary>
    /// URL route segment for service neighborhood pages (e.g., "s" in "/s/{service}/{neighborhood}").
    /// </summary>
    public string ServicePathSegment { get; set; } = "s";

    /// <summary>
    /// URL route segment for provider canonical storefronts (e.g., "biz" in "/biz/{provider}").
    /// </summary>
    public string ProviderPathSegment { get; set; } = "biz";

    /// <summary>
    /// Cache TTL in minutes for programmatic SEO page MediatR queries (default: 60 minutes).
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// HTTP Cache-Control s-maxage header duration in seconds (default: 3600 = 1 hour).
    /// </summary>
    public int CacheSMaxAgeSeconds { get; set; } = 3600;

    /// <summary>
    /// HTTP Cache-Control stale-while-revalidate duration in seconds (default: 86400 = 24 hours).
    /// </summary>
    public int StaleWhileRevalidateSeconds { get; set; } = 86400;

    /// <summary>
    /// Public brand logo URL for Schema.org organization markup.
    /// If null or empty, falls back to GeneralSettings.DefaultLogoUrl.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Default OpenGraph and Twitter card image URL for social previews.
    /// </summary>
    public string? DefaultSocialImageUrl { get; set; }

    /// <summary>
    /// Directives for robots meta tag when a page meets quality gates.
    /// </summary>
    public string IndexRobotsDirective { get; set; } = "index, follow";

    /// <summary>
    /// Directives for robots meta tag when a page fails quality gates (thin content prevention).
    /// </summary>
    public string NoIndexRobotsDirective { get; set; } = "noindex, follow";
}
