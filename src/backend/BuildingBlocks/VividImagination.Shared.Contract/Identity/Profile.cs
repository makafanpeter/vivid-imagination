using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Shared.Contract.Identity;

public sealed class Profile
{
    /// <summary>
    ///     Sub is the unique identifier for the user.
    /// </summary>
    public string? Sub { get; set; } = string.Empty;
    
    /// <summary>
    ///     Sub is the unique identifier for the user.
    /// </summary>
    public string? EncryptedSub { get; set; } = string.Empty;

    /// <summary>
    ///     Email is the email address of the user.
    /// </summary>
    public string? Email { get; set; } = string.Empty;

    /// <summary>
    ///     FullName is the full name of the user.
    /// </summary>
    public string? FullName { get; set; } = string.Empty;

    /// <summary>
    ///     PreferredUsername is the preferred username of the user.
    /// </summary>
    public string? PreferredUsername { get; set; } = string.Empty;

    /// <summary>
    ///     Boolean value indicating if the email is verified.
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;
    
    /// <summary>
    ///   Boolean value indicating if the phone is verified.
    /// </summary>
    public bool IsPhoneVerified { get; set; }  = false;

    public string? PhoneNumber { get; set; }
    public string? PhoneNumberCountryCode { get; set; }

    public string? UserType { get; set; } = string.Empty;
    public string? OrganizationId { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    
    public List<string>? Permissions { get; set; }
    public string? Role { get; set; } = string.Empty;
    public List<string>? Scopes { get; set; }
    public string? TimeZoneId { get; set; }
    public SafeBrowsingMode SafeBrowsingMode { get; set; } = SafeBrowsingMode.BlurSensitive;
}
