using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.Web.Helpers;

namespace VividImagination.Shared.Infrastructure.Web;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env):IExceptionHandler
{
    
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Resolve scoped services from HttpContext
        var webHelper = httpContext.RequestServices.GetRequiredService<IWebHelper>();
        var currentUserService = httpContext.RequestServices.GetRequiredService<ICurrentUser>();

         var level = exception switch
        {
            NotFoundException => LogLevel.Warning,
            ValidationException => LogLevel.Warning,
            UnAuthorizedException => LogLevel.Warning,
            ForbiddenException => LogLevel.Warning,
            BadRequestException => LogLevel.Warning,
            ConflictException => LogLevel.Warning,
            ArgumentException => LogLevel.Warning,
            _ => LogLevel.Error
        };

        var user = currentUserService.CurrentUser;
        var userInfo = new
        {
            userId = user?.Sub, user?.PreferredUsername
        };

        var log = new
        {
            Environment = env.EnvironmentName,
            Severity = level.ToString(),
            User = userInfo,
            httpContext.Request.Path,
            httpContext.Request.Host,
            HTTPMethod = httpContext.Request.Method,
            IPAddress = webHelper.GetCurrentIpAddress(),
            UserAgent = webHelper.GetUserAgent(),
            ErrorMessage = exception is AppException appException ? appException.ToString() : exception.Message,
            ErrorType = exception.GetType().Name,
            TraceId = Activity.Current?.Id ?? httpContext.TraceIdentifier
        };

        logger.Log(level, new EventId(exception.HResult), exception, "({@log})", log);

        // Create ProblemDetails based on exception type
        var problemDetails = exception switch
        {
            NotFoundException e => CreateProblemDetails(
                title: "Resource Not Found",
                status: HttpStatusCode.NotFound,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"),

            ValidationException e => CreateValidationProblemDetails(e),

            UnAuthorizedException e => CreateProblemDetails(
                title: "Unauthorized",
                status: HttpStatusCode.Unauthorized,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1"),

            ForbiddenException e => CreateProblemDetails(
                title: "Forbidden",
                status: HttpStatusCode.Forbidden,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3"),

            BadRequestException e => CreateProblemDetails(
                title: "Bad Request",
                status: HttpStatusCode.BadRequest,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"),

            ConflictException e => CreateProblemDetails(
                title: "Conflict",
                status: HttpStatusCode.Conflict,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"),

            ServiceUnavailableException e => CreateProblemDetails(
                title: "Service Unavailable",
                status: HttpStatusCode.ServiceUnavailable,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4"),

            SystemErrorException e => CreateProblemDetails(
                title: "Internal Server Error",
                status: HttpStatusCode.InternalServerError,
                detail: e.Message,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"),

            _ => CreateProblemDetails(
                title: "Internal Server Error",
                status: HttpStatusCode.InternalServerError,
                detail: env.IsDevelopment() || env.IsEnvironment("Testing") || exception is AppException ? exception.ToString() : "An unexpected error occurred.",
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1")
        };

        // Add common extensions
        AddCommonExtensions(problemDetails, httpContext, webHelper, currentUserService);

        // Write response
        await WriteProblemDetailsResponseAsync(httpContext, problemDetails, cancellationToken);

        return true; // Exception handled
    }
    
     private ProblemDetails CreateProblemDetails(
        string title,
        HttpStatusCode status,
        string detail,
        string type)
    {
        return new ProblemDetails
        {
            Title = title,
            Status = (int)status,
            Detail = detail,
            Type = type,
            Instance = null
        };
    }

    private ProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var validationProblem = new ValidationProblemDetails
        {
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred. ",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Instance = null
        };

        // Map validation failures to errors dictionary
        if (exception.Failures.Any())
        {
            foreach (var error in exception.Failures)
            {
                validationProblem.Errors[error.Key] = error.Value.ToArray();
            }
        }

        return validationProblem;
    }

    private void AddCommonExtensions(ProblemDetails problemDetails, 
        HttpContext httpContext,
        IWebHelper webHelper,
        ICurrentUser currentUserService)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");
        problemDetails.Extensions["path"] = httpContext.Request.Path.Value;
        problemDetails.Extensions["method"] = httpContext.Request.Method;

        // Add user info if available
        if (currentUserService.CurrentUser != null)
        {
            problemDetails.Extensions["userId"] = currentUserService.CurrentUser.Sub;
        }

        // Only include sensitive info in development
        if (env.IsDevelopment())
        {
            problemDetails.Extensions["ipAddress"] = webHelper.GetCurrentIpAddress();
            problemDetails.Extensions["userAgent"] = webHelper.GetUserAgent();
            problemDetails.Extensions["exceptionType"] = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error.GetType().Name;

            // Optionally add stack trace in development
            var stackTrace = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error.StackTrace;
            if (stackTrace != null)
            {
                problemDetails.Extensions["stackTrace"] = stackTrace;
            }
        }
    }

    private async Task WriteProblemDetailsResponseAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = env.IsDevelopment(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (problemDetails is ValidationProblemDetails details)
        {
            await httpContext.Response.WriteAsJsonAsync( details, options, cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync( problemDetails, options, cancellationToken);
        }
        
    }
}


public class ForbiddenObjectResult : ObjectResult
{
    public ForbiddenObjectResult(object error)
        : base(error)
    {
        StatusCode = StatusCodes.Status403Forbidden;
    }
}

public class UnAuthorizedObjectResult : ObjectResult
{
    public UnAuthorizedObjectResult(object error)
        : base(error)
    {
        StatusCode = StatusCodes.Status401Unauthorized;
    }
}
