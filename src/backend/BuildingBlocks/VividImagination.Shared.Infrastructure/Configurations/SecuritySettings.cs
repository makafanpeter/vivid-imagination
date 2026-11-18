namespace VividImagination.Shared.Infrastructure.Configurations;

public class SecuritySettings : ISettings
{
    // Password policy
    public int PasswordMinimumLength { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireDigit { get; set; }

    // Lockout & failed attempts
    public int FailedPasswordAllowedAttempts { get; set; }
    public int FailedPasswordLockoutMinutes { get; set; }

    // Identity & user rules
    public bool RequireUniqueEmail { get; set; }
}
