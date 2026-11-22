using VividImagination.Mobile.OpenApi;

namespace VividImagination.Mobile.Services;

public interface IFeedTelemetryService
{
    Task RecordInteractionAsync(
        Guid photoId,
        Guid creatorId,
        string interactionType,
        int feedPosition = 0,
        int dwellDurationMs = 0,
        IEnumerable<string>? tags = null,
        CancellationToken ct = default);
}

public sealed class FeedTelemetryService(
    IVividImaginationClient apiClient,
    IAuthService authService) : IFeedTelemetryService
{
    public async Task RecordInteractionAsync(
        Guid photoId,
        Guid creatorId,
        string interactionType,
        int feedPosition = 0,
        int dwellDurationMs = 0,
        IEnumerable<string>? tags = null,
        CancellationToken ct = default)
    {
        var user = authService.CurrentUser;
        if (user == null || !user.IsPersonalisationEnabled)
        {
            // Only record telemetry when user is authenticated and personalization is enabled
            return;
        }

        try
        {
            await apiClient.RecordFeedInteractionAsync(
                photoId,
                creatorId,
                interactionType,
                feedPosition,
                dwellDurationMs,
                tags?.ToList(),
                ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FeedTelemetry: Failed to record {interactionType} for photo {photoId}: {ex.Message}");
        }
    }
}
