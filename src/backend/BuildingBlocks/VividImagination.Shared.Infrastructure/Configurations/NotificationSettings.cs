namespace VividImagination.Shared.Infrastructure.Configurations;

/// <summary>
/// Settings for the Notification module — outbound email and SMS dispatch.
/// </summary>
public class NotificationSettings
{
    /// <summary>Default sender display name used in outbound emails.</summary>
    public string DefaultFromName { get; set; } = "Adinova";

    /// <summary>Default sender email address used in outbound emails.</summary>
    public string DefaultFromEmail { get; set; } = "no-reply@adinova.tech";

    /// <summary>SMTP settings for the built-in email provider.</summary>
    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
}
