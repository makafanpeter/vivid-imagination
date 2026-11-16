namespace VividImagination.Shared.Infrastructure.Configurations;

public class  SecuritySettings:ISettings
{
    // Password policy
    public int PasswordMinimumLength { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireDigit { get; set; }
    public int PasswordLifetimeDays { get; set; }

    // PIN policy
    public int PinLength { get; set; }
    public int FailedPinAllowedAttempts { get; set; }
    public int PinRecoveryLinkDaysValid { get; set; }

    // Lockout & failed attempts
    public int FailedPasswordAllowedAttempts { get; set; }
    public int FailedPasswordLockoutMinutes { get; set; }

    //  Link validity / account lifecycle
    public int AccountActivationLinkDaysValid { get; set; }
    public int PasswordRecoveryLinkDaysValid { get; set; }
    public int AllowBankInformationChangeDays { get; set; }

    // Identity & user rules
    public bool RequireUniqueEmail { get; set; }
    
   
}