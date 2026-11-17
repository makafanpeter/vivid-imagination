using System.Net.Http.Json;
using System.Text.Json;

namespace VividImagination.Mobile.Services;

public interface IFeedService
{
    Task<IReadOnlyList<FeedItem>> GetRankedFeedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public sealed class FeedService(HttpClient httpClient) : IFeedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<FeedItem>> GetRankedFeedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"{AppConfig.FeedApiUrl}?page={page}&pageSize={pageSize}";
            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<FeedItem>();
            }

            var items = await response.Content.ReadFromJsonAsync<IReadOnlyList<FeedItem>>(JsonOptions, ct);
            return items ?? Array.Empty<FeedItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching feed: {ex.Message}");
            return Array.Empty<FeedItem>();
        }
    }
}
