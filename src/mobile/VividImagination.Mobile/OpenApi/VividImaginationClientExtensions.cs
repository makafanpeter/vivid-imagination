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
    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(Models.SafeBrowsingMode safeBrowsingMode, bool isPersonalisationEnabled, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdatePersonalisationAsync(bool isPersonalisationEnabled, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task RecordFeedInteractionAsync(
        System.Guid photoId,
        System.Guid creatorId,
        string interactionType,
        int feedPosition = 0,
        int dwellDurationMs = 0,
        System.Collections.Generic.ICollection<string>? tags = null,
        System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<FileResponse> DownloadPhotoAsync(System.Guid id, string? variant, System.Threading.CancellationToken cancellationToken = default);
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

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdatePersonalisationAsync(bool isPersonalisationEnabled, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateUserPreferencesAsync(new UpdatePreferencesRequest
        {
            IsPersonalisationEnabled = isPersonalisationEnabled
        }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(Models.SafeBrowsingMode safeBrowsingMode, bool isPersonalisationEnabled, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateUserPreferencesAsync(new UpdatePreferencesRequest
        {
            SafeBrowsingMode = safeBrowsingMode.ToString(),
            IsPersonalisationEnabled = isPersonalisationEnabled
        }, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task RecordFeedInteractionAsync(
        System.Guid photoId,
        System.Guid creatorId,
        string interactionType,
        int feedPosition = 0,
        int dwellDurationMs = 0,
        System.Collections.Generic.ICollection<string>? tags = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var request = new RecordInteractionRequest
        {
            PhotoId = photoId,
            CreatorId = creatorId,
            InteractionType = interactionType,
            FeedPosition = feedPosition,
            DwellDurationMs = dwellDurationMs,
            Tags = tags
        };

        return RecordFeedInteractionAsync(request, cancellationToken);
    }

    public virtual async System.Threading.Tasks.Task<FileResponse> DownloadPhotoAsync(System.Guid id, string? variant, System.Threading.CancellationToken cancellationToken = default)
    {
        if (id == System.Guid.Empty)
            throw new System.ArgumentNullException(nameof(id));

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(BaseUrl)) urlBuilder_.Append(BaseUrl);
        // Operation Path: "api/v1/media/{id}/download"
        urlBuilder_.Append("api/v1/media/");
        urlBuilder_.Append(System.Uri.EscapeDataString(ConvertToString(id, System.Globalization.CultureInfo.InvariantCulture)));
        urlBuilder_.Append("/download");

        if (!string.IsNullOrWhiteSpace(variant))
        {
            urlBuilder_.Append('?').Append("variant=").Append(System.Uri.EscapeDataString(variant));
        }

        var client_ = _httpClient;
        var disposeClient_ = false;
        try
        {
            using (var request_ = new System.Net.Http.HttpRequestMessage())
            {
                request_.Method = new System.Net.Http.HttpMethod("GET");
                request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));

                var url_ = urlBuilder_.ToString();
                request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);

                var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                var disposeResponse_ = true;
                try
                {
                    var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
                    foreach (var item_ in response_.Headers)
                        headers_[item_.Key] = item_.Value;
                    if (response_.Content != null && response_.Content.Headers != null)
                    {
                        foreach (var item_ in response_.Content.Headers)
                            headers_[item_.Key] = item_.Value;
                    }

                    var status_ = (int)response_.StatusCode;
                    if (status_ == 200)
                    {
                        var responseStream_ = response_.Content == null ? System.IO.Stream.Null : await response_.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        var fileResponse_ = new FileResponse(status_, headers_, responseStream_, client_, response_);
                        disposeResponse_ = false;
                        return fileResponse_;
                    }
                    else
                    {
                        var responseData_ = response_.Content == null ? null : await response_.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
                    }
                }
                finally
                {
                    if (disposeResponse_)
                        response_.Dispose();
                }
            }
        }
        finally
        {
            if (disposeClient_)
                client_.Dispose();
        }
    }
}

public partial class UploadDocumentViewModel
{
    public string? Status
    {
        get
        {
            if (AdditionalProperties != null &&
                AdditionalProperties.TryGetValue("status", out var val))
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
