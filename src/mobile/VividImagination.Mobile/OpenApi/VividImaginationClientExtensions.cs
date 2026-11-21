namespace VividImagination.Mobile.OpenApi;

public partial interface IVividImaginationClient
{
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync();
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<UploadDocumentViewModel>> GetMyMediaAsync(System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<VoteResult> VoteOnPhotoAsync(System.Guid photoId, int voteValue, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, int reasonType = 0, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<Review> UpdateModerationVerdictAsync(System.Guid photoId, string verdict, bool blurRequired = false, string? reason = null, System.Threading.CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(int safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default);
}

public partial class VividImaginationClient
{
    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize)
    {
        return GetRankedFeedAsync(page, pageSize, null, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken)
    {
        return GetRankedFeedAsync(page, pageSize, null, cancellationToken);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize)
    {
        return GetPersonalizedFeedAsync(page, pageSize, null, System.Threading.CancellationToken.None);
    }

    public virtual System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetPersonalizedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken)
    {
        return GetPersonalizedFeedAsync(page, pageSize, null, cancellationToken);
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

    public virtual System.Threading.Tasks.Task<ReportResult> ReportPhotoAsync(System.Guid photoId, string? reason, int reasonType = 0, System.Threading.CancellationToken cancellationToken = default)
    {
        return ReportPhotoAsync(photoId, new ReportRequest { Reason = reason, ReasonType = reasonType }, cancellationToken);
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

    public virtual System.Threading.Tasks.Task<SafeBrowsingPreferenceDto> UpdateUserPreferencesAsync(int safeBrowsingMode, bool? isPersonalisationEnabled = null, System.Threading.CancellationToken cancellationToken = default)
    {
        return UpdateUserPreferencesAsync(new UpdatePreferencesRequest
        {
            SafeBrowsingMode = safeBrowsingMode,
            IsPersonalisationEnabled = isPersonalisationEnabled
        }, cancellationToken);
    }
}
