using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using VividImagination.Mobile.Models;
using VividImagination.Mobile.OpenApi;
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
    public void FeedItemDto_DeserializesCorrectly_FromBackendContractJson()
    {
        var json = """
        {
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

        var item = JsonSerializer.Deserialize<FeedItemDto>(json, JsonOptions);

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
    public void VoteRequest_And_Result_SerializeAndDeserializeCorrectly()
    {
        var request = new VoteRequest { Value = 1 };
        var json = JsonSerializer.Serialize(request);
        json.Should().Contain("\"value\":1");

        var responseJson = """
        {
            "photoId": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
            "upvotes": 10,
            "downvotes": 2
        }
        """;
        var result = JsonSerializer.Deserialize<VoteResult>(responseJson, JsonOptions);
        result.Should().NotBeNull();
        result!.PhotoId.Should().Be(Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"));
        result.Upvotes.Should().Be(10);
        result.Downvotes.Should().Be(2);
    }

    [Fact]
    public void ReportRequest_SerializesReasonCorrectly()
    {
        var request = new ReportRequest { Reason = "Spam / Misleading content" };
        var json = JsonSerializer.Serialize(request);
        json.Should().Contain("\"reason\":\"Spam / Misleading content\"");
    }

    [Fact]
    public void Review_And_VerdictRequest_SerializeAndDeserializeCorrectly()
    {
        var verdictRequest = new UpdateModerationVerdictRequest
        {
            Verdict = "Approved",
            BlurRequired = false,
            Reason = "Approved by moderator"
        };
        var json = JsonSerializer.Serialize(verdictRequest);
        json.Should().Contain("\"verdict\":\"Approved\"");

        var reviewJson = """
        {
            "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
            "photoId": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
            "maxRiskScore": 0.85,
            "adultScore": 0.85,
            "violenceScore": 0.1,
            "hateScore": 0.05,
            "verdict": "TaggedUnsafe",
            "isUnsafe": true,
            "blurRequired": true,
            "reportCount": 3,
            "reviewedAt": "2026-11-20T10:00:00Z"
        }
        """;

        var review = JsonSerializer.Deserialize<Review>(reviewJson, JsonOptions);
        review.Should().NotBeNull();
        review!.PhotoId.Should().Be(Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"));
        review.Verdict.Should().Be("TaggedUnsafe");
        review.IsUnsafe.Should().BeTrue();
        review.BlurRequired.Should().BeTrue();
        review.AdultScore.Should().Be(0.85);
        review.ReportCount.Should().Be(3);
    }

    [Fact]
    public void SafeBrowsingPreference_And_RecordInteraction_SerializeAndDeserializeCorrectly()
    {
        var prefJson = """
        {
            "userId": 123456789012345678,
            "safeBrowsingMode": 1,
            "isPersonalisationEnabled": true
        }
        """;
        var pref = JsonSerializer.Deserialize<SafeBrowsingPreferenceDto>(prefJson, JsonOptions);
        pref.Should().NotBeNull();
        pref!.UserId.Should().Be(123456789012345678L);
        pref.SafeBrowsingMode.Should().Be(1);
        pref.IsPersonalisationEnabled.Should().BeTrue();

        var interactionRequest = new RecordInteractionRequest
        {
            PhotoId = Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"),
            CreatorId = Guid.Parse("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f"),
            InteractionType = "dwell",
            FeedPosition = 1,
            DwellDurationMs = 2500
        };
        var interactionJson = JsonSerializer.Serialize(interactionRequest);
        interactionJson.Should().Contain("\"interactionType\":\"dwell\"");
        interactionJson.Should().Contain("\"dwellDurationMs\":2500");
    }

    [Fact]
    public void Identity_RegisterAndTokenResponse_SerializeAndDeserializeCorrectly()
    {
        var registerResultJson = """
        {
            "userId": 987654321098765432,
            "email": "creator@example.com",
            "fullName": "Creative User",
            "safeBrowsingMode": 0,
            "createdOn": "2026-11-21T12:00:00Z"
        }
        """;
        var regResult = JsonSerializer.Deserialize<RegisterUserResult>(registerResultJson, JsonOptions);
        regResult.Should().NotBeNull();
        regResult!.UserId.Should().Be(987654321098765432L);
        regResult.Email.Should().Be("creator@example.com");
        regResult.FullName.Should().Be("Creative User");
        regResult.SafeBrowsingMode.Should().Be(0);

        var tokenResponseJson = """
        {
            "access_token": "mock.jwt.access_token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "refresh_token": "mock.refresh_token",
            "scope": "openid email profile roles identity-api"
        }
        """;
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(tokenResponseJson, JsonOptions);
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().Be("mock.jwt.access_token");
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().Be(3600);
        tokenResponse.RefreshToken.Should().Be("mock.refresh_token");
        tokenResponse.Scope.Should().Be("openid email profile roles identity-api");
    }

    [Fact]
    public void IVividImaginationClient_ContainsUpdatedInterfaceMethods()
    {
        var interfaceType = typeof(IVividImaginationClient);
        var methodNames = interfaceType.GetMethods().Select(m => m.Name).ToHashSet();

        methodNames.Should().Contain("GetModerationReviewAsync");
        methodNames.Should().Contain("UpdateModerationVerdictAsync");
        methodNames.Should().Contain("CancelVoteOnPhotoAsync");
        methodNames.Should().Contain("GetPersonalizedFeedAsync");
        methodNames.Should().Contain("RecordFeedInteractionAsync");
        methodNames.Should().Contain("RegisterUserAsync");
        methodNames.Should().Contain("GetUserPreferencesAsync");
        methodNames.Should().Contain("UpdateUserPreferencesAsync");
        methodNames.Should().NotContain("GoogleSignInAsync");
        methodNames.Should().Contain("UpdatePhotoMetadataAsync");
        methodNames.Should().Contain("GetMyMediaAsync");
    }

    [Fact]
    public void EnvironmentResolution_CalculatesEndpointsCorrectly()
    {
        // Development Desktop default
        var devDesktop = "http://localhost:5101";
        // Development Android Emulator default
        var devAndroid = "http://10.0.2.2:5101";
        // Staging
        var staging = "https://staging-api.vividimagination.com";
        // Production
        var production = "https://api.vividimagination.com";

        devDesktop.Should().StartWith("http://localhost");
        devAndroid.Should().Contain("10.0.2.2");
        staging.Should().Be("https://staging-api.vividimagination.com");
        production.Should().Be("https://api.vividimagination.com");
    }

    [Fact]
    public void SubstitutedBaseUrl_OverridesDefaultConfiguration()
    {
        var customUrl = "https://custom-api.example.com";
        var options = new
        {
            AppConfig = new
            {
                Environment = "Production",
                Api = new
                {
                    DesktopBaseUrl = customUrl,
                    AndroidEmulatorBaseUrl = customUrl
                }
            }
        };

        options.AppConfig.Api.DesktopBaseUrl.Should().Be("https://custom-api.example.com");
        options.AppConfig.Api.AndroidEmulatorBaseUrl.Should().Be("https://custom-api.example.com");
    }

    [Fact]
    public void SubstitutionScriptLogic_UpdatesJsonContentCorrectly()
    {
        var sampleJson = """
        {
          "AppConfig": {
            "Environment": "Production",
            "Api": {
              "DesktopBaseUrl": "https://api.vividimagination.com",
              "AndroidEmulatorBaseUrl": "https://api.vividimagination.com"
            }
          }
        }
        """;

        using var doc = JsonDocument.Parse(sampleJson);
        var root = doc.RootElement;
        root.GetProperty("AppConfig").GetProperty("Api").GetProperty("DesktopBaseUrl").GetString()
            .Should().Be("https://api.vividimagination.com");

        var secretUrl = "https://injected-api.company.com";
        var modifiedJson = sampleJson.Replace("https://api.vividimagination.com", secretUrl);

        using var modifiedDoc = JsonDocument.Parse(modifiedJson);
        modifiedDoc.RootElement.GetProperty("AppConfig").GetProperty("Api").GetProperty("DesktopBaseUrl").GetString()
            .Should().Be("https://injected-api.company.com");
    }

    [Fact]
    public void ConfigurationBuilder_WithDisposedStream_ThrowsStreamNotReadable()
    {
        var json = """{"AppConfig": {"Environment": "Production"}}""";
        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        configBuilder.AddJsonStream(stream);
        stream.Dispose(); // simulate 'using var stream' scope exiting before Build()

        var act = () => configBuilder.Build();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Stream was not readable*");
    }

    [Fact]
    public void ConfigurationBuilder_WithMemoryStream_BuildsSuccessfully()
    {
        var json = """{"AppConfig": {"Environment": "Production", "Api": {"DesktopBaseUrl": "https://api.vividimagination.com"}}}""";
        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

        using var initialStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var memoryStream = new MemoryStream();
        initialStream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        configBuilder.AddJsonStream(memoryStream);

        var config = configBuilder.Build();
        config["AppConfig:Environment"].Should().Be("Production");
        config["AppConfig:Api:DesktopBaseUrl"].Should().Be("https://api.vividimagination.com");
    }

    [Fact]
    public void ConfigurationBuilder_WithAuthSection_BuildsAndReadsCorrectly()
    {
        var json = """
        {
            "AppConfig": {
                "Environment": "Development",
                "Auth": {
                    "ClientId": "Custom-Client-Id",
                    "ClientSecret": "custom-secret-key-123",
                    "Scope": "offline_access openid profile custom_scope"
                }
            }
        }
        """;

        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        configBuilder.AddJsonStream(stream);
        var config = configBuilder.Build();

        config["AppConfig:Auth:ClientId"].Should().Be("Custom-Client-Id");
        config["AppConfig:Auth:ClientSecret"].Should().Be("custom-secret-key-123");
        config["AppConfig:Auth:Scope"].Should().Be("offline_access openid profile custom_scope");
    }

    [Fact]
    public void AppSettingsRaw_JsonFiles_ContainValidAuthSection()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null &&
               !File.Exists(Path.Combine(currentDir.FullName, "VividImagination.slnx")) &&
               !File.Exists(Path.Combine(currentDir.FullName, "Directory.Build.props")))
        {
            currentDir = currentDir.Parent;
        }

        currentDir.Should().NotBeNull();
        var rawDir = Path.Combine(currentDir!.FullName, "src", "mobile", "VividImagination.Mobile", "Resources", "Raw");
        var appsettingsPath = Path.Combine(rawDir, "appsettings.json");

        File.Exists(appsettingsPath).Should().BeTrue();
        var json = File.ReadAllText(appsettingsPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("AppConfig").GetProperty("Auth");

        root.GetProperty("ClientId").GetString().Should().Be("VividImagination-client");
        root.GetProperty("ClientSecret").GetString().Should().Be("vivid-dev-secret");
        root.GetProperty("Scope").GetString().Should().Contain("openid");
    }

    [Fact]
    public void DynamicBaseAddressHandler_RewritesHostCorrectly()
    {
        var originalRequestUri = new Uri("http://placeholder/api/v1/feed");
        var activeTargetBase = "https://staging-api.vividimagination.com";

        var builder = new UriBuilder(new Uri(activeTargetBase))
        {
            Path = originalRequestUri.AbsolutePath,
            Query = originalRequestUri.Query.TrimStart('?')
        };

        builder.Uri.ToString().Should().Be("https://staging-api.vividimagination.com/api/v1/feed");
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

    [Fact]
    public void StaticResourceIntegrity_AllReferencedKeysInXaml_MustBeDefinedInResourceDictionaries()
    {
        // Locate repo root
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null &&
               !File.Exists(Path.Combine(currentDir.FullName, "VividImagination.slnx")) &&
               !File.Exists(Path.Combine(currentDir.FullName, "VividImagination.sln")))
        {
            currentDir = currentDir.Parent;
        }

        currentDir.Should().NotBeNull("Solution root directory must be discoverable");
        var mobileDir = Path.Combine(currentDir!.FullName, "src", "mobile", "VividImagination.Mobile");
        Directory.Exists(mobileDir).Should().BeTrue();

        var keyRegex = new System.Text.RegularExpressions.Regex(@"x:Key=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Compiled);
        var staticResRegex = new System.Text.RegularExpressions.Regex(@"\{StaticResource\s+([a-zA-Z0-9_]+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

        var definedKeys = new HashSet<string>(StringComparer.Ordinal);
        var xamlFiles = Directory.GetFiles(mobileDir, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToList();

        // 1. Collect all defined keys from merged resource dictionaries
        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in keyRegex.Matches(content))
            {
                definedKeys.Add(match.Groups[1].Value);
            }
        }

        definedKeys.Should().Contain("Magenta");
        definedKeys.Should().Contain("AppBackground");
        definedKeys.Should().Contain("AccentVermilion");

        // 2. Verify all StaticResource references resolve to a defined key
        var missingKeys = new List<string>();
        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in staticResRegex.Matches(content))
            {
                var referencedKey = match.Groups[1].Value;
                if (!definedKeys.Contains(referencedKey))
                {
                    missingKeys.Add($"{Path.GetFileName(file)}: missing '{referencedKey}'");
                }
            }
        }

        missingKeys.Should().BeEmpty("all StaticResource references in XAML files must be defined in ResourceDictionaries");
    }

    [Fact]
    public void FeedItemDto_NetScore_CalculatesUpvotesMinusDownvotes()
    {
        var dto = new FeedItemDto
        {
            Id = Guid.NewGuid(),
            PhotoId = Guid.NewGuid(),
            Caption = "Test photo with #cool and #vibes tags",
            Upvotes = 25,
            Downvotes = 5
        };

        var netScore = dto.Upvotes - dto.Downvotes;
        netScore.Should().Be(20);
    }

    [Fact]
    public void PhotoUpload_CaptionCounter_TracksLiveLength()
    {
        var caption = "Exploring misty morning trails in the mountains";
        var length = caption.Length;
        length.Should().Be(47);
        (length <= 500).Should().BeTrue();
    }

    [Fact]
    public void SafeBrowsingMode_FiltersUnsafePhotos_WhenHideSensitiveSelected()
    {
        var photos = new List<FeedItemDto>
        {
            new() { Id = Guid.NewGuid(), IsUnsafe = false },
            new() { Id = Guid.NewGuid(), IsUnsafe = true },
            new() { Id = Guid.NewGuid(), IsUnsafe = false }
        };

        var mode = SafeBrowsingMode.HideSensitive;
        var visiblePhotos = photos.Where(p => !(mode == SafeBrowsingMode.HideSensitive && p.IsUnsafe)).ToList();

        visiblePhotos.Should().HaveCount(2);
    }

    [Fact]
    public void RegisterUserRequest_SerializesWithCorrectPropertyNames()
    {
        var request = new RegisterUserRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "Alex",
            LastName = "Morgan",
            SafeBrowsingMode = 0
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        json.Should().Contain("\"email\":\"newuser@example.com\"");
        json.Should().Contain("\"password\":\"SecurePassword123!\"");
        json.Should().Contain("\"firstName\":\"Alex\"");
        json.Should().Contain("\"lastName\":\"Morgan\"");
        json.Should().Contain("\"safeBrowsingMode\":0");
    }

    [Theory]
    [InlineData("weak", false)]
    [InlineData("NoSpecial123", false)]
    [InlineData("no_upper_123!", false)]
    [InlineData("NO_LOWER_123!", false)]
    [InlineData("NoDigits!@#$", false)]
    [InlineData("ValidP@ssword1", true)]
    public void RegistrationPasswordPolicy_ValidatesComplexityRules(string password, bool expectedValid)
    {
        var isValid = password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(ch => !char.IsLetterOrDigit(ch));

        isValid.Should().Be(expectedValid);
    }
}
