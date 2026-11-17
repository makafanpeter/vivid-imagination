using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using VividImagination.Modules.Media.Contracts.Events;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class DeleteDocumentCommandHandler(
        IFileServiceManager fileServiceManager,
        IMongoRepository<FileEntry, Guid> fileRepository,
        ICurrentUser identityService,
        IAuthorizationService authorizationService,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteDocumentCommandHandler>? logger = null)
        : IRequestHandler<DeleteDocumentCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var file = await fileRepository.FindByIdAsync(request.Id, cancellationToken);

            if (file == null || file.Deleted)
            {
                throw new NotFoundException(nameof(FileEntry), request.Id);
            }

            var authResult = await authorizationService.AuthorizeAsync(
                (identityService.User ?? null)!,
                file,
                Operations.Delete);

            if (!authResult.Succeeded)
            {
                throw new ForbiddenException(identityService.CurrentUser?.PreferredUsername ?? string.Empty);
            }

            // 1. Delete primary raw file
            await fileServiceManager.DeleteAsync(file, cancellationToken);

            // 2. Cascading deletion: Prune all WebP variants (Original, Medium, Thumbnail) from storage
            if (file.Variants is { Count: > 0 })
            {
                foreach (var variant in file.Variants.Values)
                {
                    var variantEntry = new FileEntryModel
                    {
                        Id = file.Id,
                        FileLocation = variant.StorageKey,
                        ContainerName = file.ContainerName,
                        FileName = Path.GetFileName(variant.StorageKey),
                        FileExtension = Path.GetExtension(variant.StorageKey),
                        ContentType = variant.ContentType
                    };

                    try
                    {
                        await fileServiceManager.DeleteAsync(variantEntry, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Failed to prune variant {VariantName} at {StorageKey} for file {FileId}", variant.VariantName, variant.StorageKey, file.Id);
                    }
                }
            }

            file.Deleted = true;

            await fileRepository.UpdateAsync(file, cancellationToken);

            // Publish PhotoDeletedIntegrationEvent for Feed module cleanup
            await publishEndpoint.Publish(new PhotoDeletedIntegrationEvent(
                file.Id,
                file.FileLocation ?? file.FileName,
                DateTime.UtcNow), cancellationToken);

            return Unit.Value;
        }
    }
}
