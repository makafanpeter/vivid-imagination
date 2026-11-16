using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Shared.Infrastructure.Configurations;

public sealed class MfaSettings
{
    public string Issuer { get; set; } = AppConstants.AppName;

    public EmailChallengeOptions Email { get; init; } = new();

    public SmsChallengeOptions Sms { get; init; } = new();
    public int RecoveryCodesCount { get; set; } = 10;
}


public enum MFAMethod
{
    RecoveryCode = 0,
    Authenticator = 1,
    Email = 2,
    Sms = 3
}