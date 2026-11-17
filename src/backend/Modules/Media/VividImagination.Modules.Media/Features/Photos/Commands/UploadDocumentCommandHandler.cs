using System.Security.Cryptography;
using AutoMapper;
using CryptographyHelper.SymmetricAlgorithms;
using LocalChamp.Shared.Infrastructure.Web.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Modules.Media.Services.FileService;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Media.Features.Photos.Commands
{
    public class UploadDocumentCommandHandler(
        IFileServiceManager fileServiceManager,
        IHttpContextAccessor accessor,
        IMongoRepository<FileEntry, Guid> dynamoDbContext,
        IMapper mapper,
        ICurrentUser identityService,
        LinkGenerator linkGenerator,
        IWebHelper webHelper)
        : IRequestHandler<UploadDocumentCommand, UploadDocumentViewModel>
    {
        public async Task<UploadDocumentViewModel> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {

            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var appReference = identityService.GetUserId();
            var model = new FileEntryModel()
            {
                Description = request.Description,
                FileName =  request.FileName,
                Name = request.Name,
                Id = id,
                Size = request.ContentLength,
                UploadedTime = now,
                ContainerName = request.ContainerName,
                OwnerId = appReference.ToString(),
                ContentType = request.ContentType,
                FileExtension = request.FileExtension,
                SourceIp = webHelper.GetCurrentIpAddress(),
                
            };



            var key = SymmetricCrypto.GenerateKey(16);
            if (request.Encrypt)
            {
                await using var inputStream = request.ContentStream;
                await using var encryptedStream = new MemoryStream(inputStream
                    .UseAES(key)
                    .WithCipher(CipherMode.ECB)
                    .WithPadding(PaddingMode.PKCS7)
                    .Encrypt());
                await fileServiceManager.CreateAsync(model, encryptedStream, cancellationToken);
            }
            else
            {
                await using var memoryStream = request.ContentStream;
                await fileServiceManager.CreateAsync(model, memoryStream, cancellationToken);
            }


            var enforceSsl = webHelper.IsCurrentConnectionSecured();


            var scheme = enforceSsl ? "https" : accessor.HttpContext?.Request.Scheme;

            

            var url = linkGenerator.GetUriByAction(
                accessor.HttpContext,
                pathBase: accessor.HttpContext?.Request.PathBase,
                action: request.Public? "Public" :"Download",
                controller: "Files",
                host: accessor.HttpContext?.Request.Host,
                scheme: scheme,
                values: new  {id});
            
            var entry = new FileEntry()
            {
                Id = id,
                UploadedTime = now.UtcDateTime,
                Description = model.Description,
                FileLocation = model.FileLocation,
                FileName = model.FileName,
                Name = model.Name,
                Size = request.ContentLength,
                ContainerName =  model.ContainerName,
                FileExtension = request.FileExtension,
                ContentType = request.ContentType,
                OwnerId = appReference.ToString(),
                Url = url,
                SourceIp  = model.SourceIp,
                CreatedOn = DateTime.UtcNow,
            };
            await dynamoDbContext.AddAsync(entry, cancellationToken);

            var response = mapper.Map<UploadDocumentViewModel>(entry);

            return response;
        }
    }
}
