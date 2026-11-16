namespace VividImagination.Shared.Infrastructure.Configurations;

/// <summary>
/// Configuration criteria for Tier 3 Local Champion automatic elevation and retention.
/// </summary>
public class LocalChampionEligibilitySettings : ISettings
{
    public int MinCompletedJobs { get; set; } = 10;
    public decimal MinAverageRating { get; set; } = 4.70m;
    public int MaxAverageResponseTimeMinutes { get; set; } = 120;
    public bool AllowDemotionOnRegression { get; set; } = true;

    public static LocalChampionEligibilitySettings Default { get; } = new();
}
