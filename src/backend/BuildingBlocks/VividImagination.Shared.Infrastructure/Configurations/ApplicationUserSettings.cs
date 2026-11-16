namespace VividImagination.Shared.Infrastructure.Configurations;

public class ApplicationUserSettings:ISettings
{
    public int AllowBankInformationChangeDays { get; set; }
    public int AccountActivationLinkDaysValid { get; set; }
    public int PasswordRecoveryLinkDaysValid { get; set; }

    public string SecretKey { get; set; }
    public int FailedPasswordAllowedAttempts { get; set; }
    public int FailedPasswordLockoutMinutes { get; set; }
      
    public int PasswordLifetimeDays { get; set; }
        
    public int PinLength { get; set; }
    public int PasswordMinimumLength { get; set; }

    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireDigit { get; set; }
    public int PinRecoveryLinkDaysValid { get; set; }
    public string AppName { get; set; }
    public string DefaultTimeZoneId { get; set; }
    public bool AllowUsersToSetTimeZone { get; set; }
        
    public int FailedPinAllowedAttempts { get; set; }
}