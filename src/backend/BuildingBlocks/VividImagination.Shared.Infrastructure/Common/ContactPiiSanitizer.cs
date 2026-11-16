using System.Text.RegularExpressions;

namespace VividImagination.Shared.Infrastructure.Common;

public static partial class ContactPiiSanitizer
{
    private const string MaskReplacement = "[contact details hidden]";

    // Regex for Email addresses
    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    // Regex for Nigerian and international mobile numbers (including spaced / dotted formats like 0803 123 4567 or 080-312-34567)
    [GeneratedRegex(@"(?:\+?234|0)[\s.-]*[789][\s.-]*[01][\s.-]*(?:\d[\s.-]*){8}", RegexOptions.IgnoreCase)]
    private static partial Regex PhoneRegex();

    // Regex for generic 10-14 digit telephone numbers
    [GeneratedRegex(@"\b(?:\+?[1-9]\d{0,2}[\s.-]*)?(?:\(?\d{3,4}\)?[\s.-]*)?\d{3,4}[\s.-]*\d{4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex GenericPhoneRegex();

    // Regex for Direct messaging & social URLs (wa.me, api.whatsapp.com, t.me, etc.)
    [GeneratedRegex(@"(?:https?:\/\/)?(?:www\.)?(?:wa\.me|api\.whatsapp\.com|chat\.whatsapp\.com|t\.me|telegram\.me|instagram\.com|facebook\.com|fb\.me)\/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex OffPlatformLinkRegex();

    /// <summary>
    /// Checks whether the text contains prohibited contact details (phone numbers, emails, or off-platform messaging links).
    /// </summary>
    public static bool ContainsContactInfo(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return EmailRegex().IsMatch(text) ||
               PhoneRegex().IsMatch(text) ||
               OffPlatformLinkRegex().IsMatch(text);
    }

    /// <summary>
    /// Replaces prohibited phone numbers, emails, and off-platform links with a placeholder mask.
    /// </summary>
    public static string MaskContactInfo(string? text, string mask = MaskReplacement)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var result = EmailRegex().Replace(text, mask);
        result = PhoneRegex().Replace(result, mask);
        result = OffPlatformLinkRegex().Replace(result, mask);

        return result;
    }
}
