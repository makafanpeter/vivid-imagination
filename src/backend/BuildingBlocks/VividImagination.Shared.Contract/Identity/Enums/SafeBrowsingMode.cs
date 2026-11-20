using System.Text.Json.Serialization;

namespace VividImagination.Shared.Contract.Identity.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SafeBrowsingMode
{
    BlurSensitive = 0,
    HideSensitive = 1,
    AlwaysShowSensitive = 2
}
