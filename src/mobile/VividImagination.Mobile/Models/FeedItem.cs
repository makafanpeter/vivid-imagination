using System.Text.Json.Serialization;

namespace VividImagination.Mobile.Models;

public sealed class FeedItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("photoId")]
    public Guid PhotoId { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("storageKey")]
    public string StorageKey { get; set; } = string.Empty;

    [JsonPropertyName("blurHash")]
    public string BlurHash { get; set; } = string.Empty;

    [JsonPropertyName("caption")]
    public string Caption { get; set; } = string.Empty;

    [JsonPropertyName("upvotes")]
    public int Upvotes { get; set; }

    [JsonPropertyName("downvotes")]
    public int Downvotes { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("isUnsafe")]
    public bool IsUnsafe { get; set; }

    [JsonPropertyName("blurRequired")]
    public bool BlurRequired { get; set; }

    [JsonPropertyName("safetyCategory")]
    public string? SafetyCategory { get; set; }

    [JsonPropertyName("sensitiveContentWarning")]
    public string? SensitiveContentWarning { get; set; }

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }
}
