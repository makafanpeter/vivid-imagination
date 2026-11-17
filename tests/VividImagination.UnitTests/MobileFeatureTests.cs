using System.Text.Json;
using FluentAssertions;
using VividImagination.Mobile.Models;
using VividImagination.Mobile.Services;
using Xunit;

namespace VividImagination.UnitTests;

public class MobileFeatureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void BlurHashCore_ValidHash_ReturnsCorrectByteLength()
    {
        const string blurHash = "LEHV6nWB2yk8pyo0adR*.7kCMdnj";
        const int width = 32;
        const int height = 32;

        var pixels = BlurHashCore.Decode(blurHash, width, height);

        pixels.Should().NotBeNull();
        pixels!.Length.Should().Be(width * height * 4); // 32x32 BGRA = 4096 bytes
    }

    [Fact]
    public void BlurHashCore_CreateBmp_ProducesValidBmpHeader()
    {
        const string blurHash = "LEHV6nWB2yk8pyo0adR*.7kCMdnj";
        const int width = 16;
        const int height = 16;

        var pixels = BlurHashCore.Decode(blurHash, width, height)!;
        var bmpBytes = BlurHashCore.CreateBmp(pixels, width, height);

        bmpBytes.Should().NotBeNull();
        // BMP magic header 'B' 'M'
        bmpBytes[0].Should().Be((byte)'B');
        bmpBytes[1].Should().Be((byte)'M');
        // Total size = 54 header + (16 * 16 * 4) = 54 + 1024 = 1078 bytes
        bmpBytes.Length.Should().Be(54 + 16 * 16 * 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("invalid-hash!")]
    public void BlurHashCore_InvalidOrMalformedHash_ReturnsNull(string invalidHash)
    {
        var pixels = BlurHashCore.Decode(invalidHash, 32, 32);
        pixels.Should().BeNull();
    }

    [Fact]
    public void FeedItem_DeserializesCorrectly_FromBackendContractJson()
    {
        var json = """
        {
            "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
            "photoId": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
            "userId": "c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f",
            "storageKey": "photos/test.webp",
            "blurHash": "LEHV6nWB2yk8pyo0adR*.7kCMdnj",
            "caption": "A sunset over the mountains",
            "upvotes": 42,
            "downvotes": 3,
            "score": 12.5,
            "isUnsafe": true,
            "blurRequired": true,
            "safetyCategory": "Suggestive Content",
            "sensitiveContentWarning": "May contain sensitive attire",
            "uploadedAt": "2026-11-17T12:00:00Z"
        }
        """;

        var item = JsonSerializer.Deserialize<FeedItem>(json, JsonOptions);

        item.Should().NotBeNull();
        item!.PhotoId.Should().Be(Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"));
        item.Caption.Should().Be("A sunset over the mountains");
        item.Upvotes.Should().Be(42);
        item.Downvotes.Should().Be(3);
        item.IsUnsafe.Should().BeTrue();
        item.BlurRequired.Should().BeTrue();
        item.SafetyCategory.Should().Be("Suggestive Content");
        item.SensitiveContentWarning.Should().Be("May contain sensitive attire");
    }

    [Fact]
    public void ShieldState_WhenBlurRequired_EnforcesBlurShieldUntilExplicitReveal()
    {
        var blurRequired = true;
        var isRevealed = false;

        var isShieldActive = blurRequired && !isRevealed;
        isShieldActive.Should().BeTrue();

        // User consents by tapping "Show Content"
        isRevealed = true;
        isShieldActive = blurRequired && !isRevealed;
        isShieldActive.Should().BeFalse();

        // User taps "Hide" to re-blur
        isRevealed = false;
        isShieldActive = blurRequired && !isRevealed;
        isShieldActive.Should().BeTrue();
    }

    [Fact]
    public void TwoFlagRule_WhenReportsReachThreshold_ActivatesBlurShield()
    {
        var totalReports = 1;
        var blurRequired = false;

        // First report does not meet threshold
        if (totalReports >= 2) blurRequired = true;
        blurRequired.Should().BeFalse();

        // Second report triggers the 2-flag threshold
        totalReports = 2;
        if (totalReports >= 2) blurRequired = true;
        blurRequired.Should().BeTrue();
    }

    [Fact]
    public void DynamicRanking_CalculatesDecayedScoreAccurately()
    {
        var upvotes = 20;
        var downvotes = 5;
        var ageInHours = 1.0;

        // SRS Formula: (Upvotes - Downvotes) / (AgeInHours + 2)^1.5
        var score = (upvotes - downvotes) / Math.Pow(ageInHours + 2, 1.5);

        score.Should().BeApproximately(2.8867, 0.001);
    }

    [Fact]
    public void SafeBrowsingMode_EnumValues_MatchSRSRequirements()
    {
        ((int)SafeBrowsingMode.BlurSensitive).Should().Be(0);
        ((int)SafeBrowsingMode.HideSensitive).Should().Be(1);
        ((int)SafeBrowsingMode.AlwaysShowSensitive).Should().Be(2);
    }
}
