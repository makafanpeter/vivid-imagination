using System.Security.Cryptography;
using AutoMapper;
using CryptographyHelper.SymmetricAlgorithms;
using VividImagination.Shared.Infrastructure.Web.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Mongo;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Media.Features.Photos.Commands;

public class UpdateDocumentCommandHandler (IMongoRepository<FileEntry, Guid> dynamoDbContext,
    IMapper mapper,
    ICurrentUser identityService,
    IAuthorizationService authorizationService,
    IWebHelper webHelper,
    IFileServiceManager fileServiceManager ,
    LinkGenerator linkGenerator,
    IHttpContextAccessor accessor)
    
    : IRequestHandler<UpdateDocumentCommand, UploadDocumentViewModel>
{
    public async Task<UploadDocumentViewModel> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var file = await dynamoDbContext.FindByIdAsync(request.Id, cancellationToken);
        if (file == null || file.Deleted)
        {
            throw new NotFoundException(nameof(FileEntry), request.Id);
        }
        
        var authorizationResult = await authorizationService.AuthorizeAsync((identityService.User ?? null)!, file, Operations.Update);
        if (!authorizationResult.Succeeded)
        {
            throw new ForbiddenException(identityService.GetUserName());
        }
        
        
        var now = DateTimeOffset.UtcNow;
        var appReference = identityService.GetUserId();
        var model = new FileEntryModel()
        {
            Description = request.Description,
            FileName =  request.FileName,
            Name = request.Name,
            Id = file.Id,
            Size = request.ContentLength,
            UploadedTime = now,
            ContainerName = request.ContainerName,
            OwnerId = appReference.ToString(),
            ContentType = request.ContentType,
            FileExtension = request.FileExtension,
            SourceIp = webHelper.GetCurrentIpAddress(),
        };
        
            
        await fileServiceManager.DeleteAsync(file, cancellationToken);
            
        await using var memoryStream = request.ContentStream;
        await fileServiceManager.CreateAsync(model, memoryStream, cancellationToken);

        
        
        string? url = null;
        if (accessor.HttpContext != null)
        {
            var enforceSsl = webHelper.IsCurrentConnectionSecured();
            var scheme = enforceSsl ? "https" : accessor.HttpContext.Request.Scheme;

            url = linkGenerator.GetUriByAction(
                accessor.HttpContext,
                pathBase: accessor.HttpContext.Request.PathBase,
                action: request.Public ? "Public" : "Download",
                controller: "Files",
                host: accessor.HttpContext.Request.Host,
                scheme: scheme,
                values: new { request.Id });
        }

        // Update Fields
        file.Description = request.Description;
        
        file.FileName = request.FileName;
        file.FileExtension = request.FileExtension;
        file.Size = request.ContentLength;
        file.ContentType = request.ContentType;
        file.ContainerName = request.ContainerName;
        file.Name = request.Name;
        file.SourceIp = webHelper.GetCurrentIpAddress();
        file.Url = url;
        file.FileLocation = model.FileLocation;
        
        
        
        await  dynamoDbContext.UpdateAsync(file, cancellationToken);
        return mapper.Map<UploadDocumentViewModel>(file);
    }
}