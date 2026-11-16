using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VividImagination.Modules.Media.Entities;
using VividImagination.Modules.Media.Persistence;
using VividImagination.Modules.Media.Services;
using VividImagination.Modules.Media.Contracts.Events;

namespace VividImagination.Modules.Media.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly MediaDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public MediaController(MediaDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public record UploadPhotoRequest(Guid UserId, string? Caption, string? StorageKey, string? BlurHash, long? FileSize);
    public record PresignedUrlRequest(Guid UserId, string FileName, string ContentType);
    public record PresignedUrlResponse(string UploadUrl, string StorageKey);

    [HttpPost("presigned-upload")]
    public ActionResult<PresignedUrlResponse> GetPresignedUploadUrl([FromBody] PresignedUrlRequest request)
    {
        var key = $"photos/{request.UserId}/{Guid.NewGuid()}_{request.FileName}";
        var simulatedUrl = $"https://s3.amazonaws.com/vivid-imagination-photos/{key}";
        return Ok(new PresignedUrlResponse(simulatedUrl, key));
    }

    [HttpPost("upload")]
    public async Task<ActionResult<Photo>> UploadPhoto([FromBody] UploadPhotoRequest request, CancellationToken ct)
    {
        var photoId = Guid.NewGuid();
        var storageKey = request.StorageKey ?? $"photos/{request.UserId}/{photoId}.jpg";
        var blurHash = request.BlurHash ?? BlurHashGenerator.Generate(storageKey);

        var photo = new Photo
        {
            Id = photoId,
            UserId = request.UserId,
            StorageKey = storageKey,
            BlurHash = blurHash,
            Caption = request.Caption ?? string.Empty,
            ContentType = "image/jpeg",
            FileSize = request.FileSize ?? 1024 * 1024,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _dbContext.Photos.Add(photo);
        await _dbContext.SaveChangesAsync(ct);

        // Publish event to MassTransit (Moderation and Feed modules consume this)
        await _publishEndpoint.Publish(new PhotoUploadedIntegrationEvent(
            photo.Id,
            photo.UserId,
            photo.StorageKey,
            photo.BlurHash,
            photo.Caption,
            photo.CreatedAt), ct);

        return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, photo);
    }

    [HttpGet("photos/{id:guid}")]
    public async Task<ActionResult<Photo>> GetPhoto(Guid id, CancellationToken ct)
    {
        var photo = await _dbContext.Photos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (photo == null)
        {
            return NotFound();
        }

        return Ok(photo);
    }

    [HttpDelete("photos/{id:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, CancellationToken ct)
    {
        var photo = await _dbContext.Photos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (photo == null)
        {
            return NotFound();
        }

        photo.IsDeleted = true;
        await _dbContext.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new PhotoDeletedIntegrationEvent(photo.Id, photo.StorageKey, DateTime.UtcNow), ct);

        return NoContent();
    }
}
