using System.Net.Http.Json;
using System.Text.Json;

namespace VividImagination.Mobile.Services;

public interface ISocialService
{
    Task<VoteResult?> VoteAsync(Guid photoId, int value, CancellationToken ct = default);
    Task<VoteResult?> GetVoteAsync(Guid photoId, CancellationToken ct = default);
    Task<ReportResult?> ReportPhotoAsync(Guid photoId, string? reason, CancellationToken ct = default);
}

public sealed class SocialService(HttpClient httpClient) : ISocialService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<VoteResult?> VoteAsync(Guid photoId, int value, CancellationToken ct = default)
    {
        try
        {
            var url = AppConfig.GetPhotoVoteUrl(photoId);
            var response = await httpClient.PostAsJsonAsync(url, new VoteRequest(value), JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<VoteResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error voting: {ex.Message}");
            return null;
        }
    }

    public async Task<VoteResult?> GetVoteAsync(Guid photoId, CancellationToken ct = default)
    {
        try
        {
            var url = AppConfig.GetPhotoVoteUrl(photoId);
            var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<VoteResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting vote: {ex.Message}");
            return null;
        }
    }

    public async Task<ReportResult?> ReportPhotoAsync(Guid photoId, string? reason, CancellationToken ct = default)
    {
        try
        {
            var url = AppConfig.GetPhotoReportUrl(photoId);
            var response = await httpClient.PostAsJsonAsync(url, new ReportRequest(reason), JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReportResult>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reporting photo: {ex.Message}");
            return null;
        }
    }
}
