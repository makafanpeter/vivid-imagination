using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OpenIddict.Validation.AspNetCore;
using VividImagination.Modules.Media.Features.Photos.Commands;
using VividImagination.Modules.Media.Features.Photos.Queries;
using VividImagination.Modules.Media.Features.Photos.ViewModels;
using VividImagination.Modules.Media.Persistence.Enums;
using VividImagination.Shared.Infrastructure.Common;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Modules.Media.Controllers.v1;

public static class MediaApis
{
    public static string DownloadEndpointName = nameof(DownloadAsync);
    private static readonly AuthorizeAttribute AuthorizeAttribute = new() { AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };

    public static IEndpointRouteBuilder MapMediaApiV1(this IEndpointRouteBuilder app)
    {
        MapMediaRoutes(app);
        return app;
    }

    private static void MapMediaRoutes(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup($"{AppConstants.ApiRoute}{MediaModule.ModuleConstants.ModuleNameForRouting}")
            .WithTags(MediaModule.ModuleConstants.FormattedModuleName)
            .HasApiVersion(1.0);

        group.MapPost("", UploadAsync)
            .WithName(nameof(UploadAsync))
            .WithDescription("Upload a document")
            .RequireAuthorization(AuthorizeAttribute)
            .Accepts<UploadFile>("multipart/form-data")
            .Produces<UploadDocumentViewModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("{id:guid}/download", DownloadAsync)
            .WithName(nameof(DownloadAsync))
            .WithDescription("Download a document")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("{id:guid}", GetAsync)
            .WithName(nameof(GetAsync))
            .WithDescription("Get document metadata")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces<UploadDocumentViewModel>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("{id:guid}", DeleteAsync)
            .WithName(nameof(DeleteAsync))
            .WithDescription("Delete a document")
            .RequireAuthorization(AuthorizeAttribute)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("{id:guid}", UpdateAsync)
            .WithName(nameof(UpdateAsync))
            .WithDescription("Update a document")
            .RequireAuthorization(AuthorizeAttribute)
            .Accepts<UploadFile>("multipart/form-data")
            .Produces<UploadDocumentViewModel>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
        
        
    }

    private static async Task<IResult> UploadAsync(
        [FromForm] UploadFile? model,
        IMediator mediator,
        ILogger<MediaModule> logger,
        CancellationToken ct)
    {
        if (model?.FormFile is null)
        {
            logger.LogError("Request does not contain any file");
            throw new BadRequestException("Request does not contain any file");
        }

        var fileName = model.FormFile.FileName.Replace("\"", string.Empty);

        var request = new UploadDocumentCommand
        {
            FileName = fileName,
            ContentStream = model.FormFile.OpenReadStream(),
            ContentType = model.FormFile.ContentType,
            ContentLength = model.FormFile.Length,
            ContainerName = model.Container,
            Description = model.Description,
            Name = model.Name,
            FileExtension = Path.GetExtension(fileName),
            FileTypes = StorageFileType.Image,
            //AllowedFiles = new []{ }
        };

        var response = await mediator.Send(request, ct);

        return Results.CreatedAtRoute(nameof(GetAsync),
            new { id = response.Id },
            response);
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var response = await mediator.Send(new DownloadDocumentQuery { Id = id }, ct);

        return Results.File(
            response.Content,
            response.ContentType,
            response.FileName);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var response = await mediator.Send(new GetDocumentQuery { Id = id }, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteDocumentCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromForm] UploadFile? model,
        IMediator mediator,
        ILogger<MediaModule> logger,
        CancellationToken ct)
    {
        if (model?.FormFile is null)
        {
            logger.LogError("Request does not contain any file");
            throw new BadRequestException("Request does not contain any file");
        }

        var fileName = model.FormFile.FileName.Replace("\"", string.Empty);

        var command = new UpdateDocumentCommand
        {
            Id = id,
            FileName = fileName,
            ContentStream = model.FormFile.OpenReadStream(),
            ContentType = model.FormFile.ContentType,
            ContentLength = model.FormFile.Length,
            ContainerName = model.Container,
            Description = model.Description,
            Name = model.Name,
            FileExtension = Path.GetExtension(fileName),
            FileTypes = StorageFileType.Image,
            //AllowedFiles = model.AllowedFiles
        };

        var response = await mediator.Send(command, ct);
        return Results.Ok(response);
    }
    
}