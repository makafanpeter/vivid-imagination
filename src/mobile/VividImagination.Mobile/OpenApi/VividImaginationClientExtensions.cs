namespace VividImagination.Mobile.OpenApi;

public partial interface IVividImaginationClient
{
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize);
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<FeedItemDto>> GetRankedFeedAsync(int? page, int? pageSize, System.Threading.CancellationToken cancellationToken);
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
}
