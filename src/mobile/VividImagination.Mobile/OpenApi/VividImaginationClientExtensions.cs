namespace VividImagination.Mobile.OpenApi;

public partial interface IVividImaginationClient
{
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode, System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode, System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync();
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync(System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<VoteResult> VoteOnPhotoAsync(System.Guid photoId, int voteValue, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, string reasonType = "Other", System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, int reasonType, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<Review> UpdateModerationVerdictAsync(System.Guid photoId, string verdict, bool blurRequired = false, string? reason = null, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(string safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(int safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(Models.SafeBrowsingMode safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default);
}

public partial class VividImaginationClient
{
    partial void Initialize()
    {
        if (_httpClient?.BaseAddress != null)
        {
            BaseUrl = _httpClient.BaseAddress.ToString();
        }
    }

    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        settings.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize)
    {
        return GetRankedFeedAsync(page, pageSize, (string?)null, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken)
    {
        return GetRankedFeedAsync(page, pageSize, (string?)null, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode)
    {
        var modeStr = safeBrowsingMode.HasValue && System.Enum.IsDefined(typeof(Models.SafeBrowsingMode), safeBrowsingMode.Value)
            ? ((Models.SafeBrowsingMode)safeBrowsingMode.Value).ToString()
            : safeBrowsingMode?.ToString();
        return GetRankedFeedAsync(page, pageSize, modeStr, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode, System.Threading.CancellationToken cancellationToken)
    {
        var modeStr = safeBrowsingMode.HasValue && System.Enum.IsDefined(typeof(Models.SafeBrowsingMode), safeBrowsingMode.Value)
            ? ((Models.SafeBrowsingMode)safeBrowsingMode.Value).ToString()
            : safeBrowsingMode?.ToString();
        return GetRankedFeedAsync(page, pageSize, modeStr, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize)
    {
        return GetPersonalizedFeedAsync(page, pageSize, (string?)null, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken)
    {
        return GetPersonalizedFeedAsync(page, pageSize, (string?)null, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode)
    {
        var modeStr = safeBrowsingMode.HasValue && System.Enum.IsDefined(typeof(Models.SafeBrowsingMode), safeBrowsingMode.Value)
            ? ((Models.SafeBrowsingMode)safeBrowsingMode.Value).ToString()
            : safeBrowsingMode?.ToString();
        return GetPersonalizedFeedAsync(page, pageSize, modeStr, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, int? safeBrowsingMode, System.Threading.CancellationToken cancellationToken)
    {
        var modeStr = safeBrowsingMode.HasValue && System.Enum.IsDefined(typeof(Models.SafeBrowsingMode), safeBrowsingMode.Value)
            ? ((Models.SafeBrowsingMode)safeBrowsingMode.Value).ToString()
            : safeBrowsingMode?.ToString();
        return GetPersonalizedFeedAsync(page, pageSize, modeStr, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync()
    {
        return GetMyMediaAsync(1, 20, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync(System.Threading.CancellationToken cancellationToken)
    {
        return GetMyMediaAsync(1, 20, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<VoteResult> VoteOnPhotoAsync(System.Guid photoId, int voteValue, System.Threading.CancellationToken cancellationToken = default)
    {
        return VoteOnPhotoAsync(photoId, new VoteRequest { Value = voteValue }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, string reasonType = "Other", System.Threading.CancellationToken cancellationToken = default)
    {
        return ReportPhotoAsync(photoId, new ReportRequest { Reason = reason, ReasonType = reasonType }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, int reasonType, System.Threading.CancellationToken cancellationToken = default)
    {
        string reasonTypeStr = reasonType switch
        {
            1 => "Spam",
            2 => "Harassment",
            3 => "HateSpeech",
            4 => "ExplicitNudity",
            5 => "Violence",
            6 => "Copyright",
            _ => "Other"
        };
        return ReportPhotoAsync(photoId, new ReportRequest { Reason = reason, ReasonType = reasonTypeStr }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<Review> UpdateModerationVerdictAsync(System.Guid photoId, string verdict, bool blurRequired = false, string? reason = null, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateModerationVerdictAsync(photoId, new UpdateModerationVerdictRequest
        {
            Verdict = verdict,
            BlurRequired = blurRequired,
            Reason = reason
        }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(string safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateUserPreferencesAsync(new UpdatePreferencesRequest
        {
            SafeBrowsingMode = safeBrowsingMode,
            IsPersonalisationEnabled = isPersonalisationEnabled
        }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(Models.SafeBrowsingMode safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateUserPreferencesAsync(safeBrowsingMode.ToString(), isPersonalisationEnabled, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(int safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default)
    {
        var modeEnum = System.Enum.IsDefined(typeof(Models.SafeBrowsingMode), safeBrowsingMode)
            ? ((Models.SafeBrowsingMode)safeBrowsingMode).ToString()
            : safeBrowsingMode.ToString();
        return UpdateUserPreferencesAsync(modeEnum, isPersonalisationEnabled, cancellationToken);
    }
}

public partial class UploadDocumentViewModel
{
    public string? BlurHash
    {
        get
        {
            if (AdditionalProperties != null &&
                AdditionalProperties.TryGetValue("blurHash", out var val))
            {
                if (val is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return element.GetString();
                }

                if (val is string s)
                {
                    return s;
                }
            }

            return null;
        }
    }
}
