namespace VividImagination.Shared.Infrastructure.Configurations;

/// <summary>
/// Configuration settings for Booking module operations.
/// </summary>
public class BookingSettings : ISettings
{
    /// <summary>
    /// Hold period in minutes for pending payment reservation locks (defaults to 5 minutes).
    /// </summary>
    public int HoldPeriodMinutes { get; set; } = 5;

    public static BookingSettings Default { get; } = new();
}
