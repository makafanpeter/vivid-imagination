using MassTransit;
using VividImagination.Modules.Media.Contracts.Queries;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Media.Features.Photos.Consumers;

public class ValidatePhotoConsumer(IMongoRepository<FileEntry, Guid> repository)
    : IConsumer<ValidatePhotoRequest>
{
    public async Task Consume(ConsumeContext<ValidatePhotoRequest> context)
    {
        var photoId = context.Message.PhotoId;
        var file = await repository.FindByIdAsync(photoId, context.CancellationToken);

        if (file is null)
        {
            await context.RespondAsync(new PhotoValidationResponse(photoId, Exists: false));
            return;
        }

        await context.RespondAsync(new PhotoValidationResponse(
            PhotoId: file.Id,
            Exists: true,
            IsDeleted: file.Deleted,
            OwnerId: file.OwnerId,
            Name: file.Name,
            Description: file.Description));
    }
}
