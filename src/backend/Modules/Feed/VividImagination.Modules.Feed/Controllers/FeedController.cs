using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using VividImagination.Modules.Feed.Persistence;
using VividImagination.Modules.Feed.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Feed.Controllers;

[ApiController]
[Route("api/v1/feed")]
public class FeedController : ControllerBase
{
    private readonly IMongoRepository<FeedItem, Guid> _repository;
    private readonly IDistributedCache _cache;

    public FeedController(IMongoRepository<FeedItem, Guid> repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public record FeedItemDto(
        Guid PhotoId,
        Guid UserId,
        string StorageKey,
        string BlurHash,
        string Caption,
        int Upvotes,
        int Downvotes,
        double Score,
        bool IsUnsafe,
        bool BlurRequired,
        string? SafetyCategory,
        string? SensitiveContentWarning,
        DateTime UploadedAt);

    [HttpGet]
    public async Task<ActionResult<List<FeedItemDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        string cacheKey = $"feed_page_{page}";

        // NFR-01: Check distributed Redis cache first (< 25ms)
        var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            var cachedItems = JsonSerializer.Deserialize<List<FeedItemDto>>(cachedJson);
            if (cachedItems != null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return Ok(cachedItems);
            }
        }

        // On miss: query MongoDB feed_items
        Response.Headers["X-Cache"] = "MISS";
        var items = await _repository.GetRankedFeedAsync(page, pageSize, ct);

        var dtos = items.Select(i => new FeedItemDto(
            i.PhotoId,
            i.UserId,
            i.StorageKey,
            i.BlurHash,
            i.Caption,
            i.Upvotes,
            i.Downvotes,
            i.Score,
            i.IsUnsafe,
            i.BlurRequired,
            i.SafetyCategory,
            i.BlurRequired ? $"Sensitive Content: {i.SafetyCategory ?? "Content Flagged"}" : null,
            i.UploadedAt
        )).ToList();

        // Write to Redis with sliding expiration
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
            SlidingExpiration = TimeSpan.FromSeconds(30)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), options, ct);

        return Ok(dtos);
    }
}
