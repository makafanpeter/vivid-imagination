using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Prometheus;
using VividImagination.Shared.Contract.Identity;

namespace VividImagination.Shared.Infrastructure.Web.Behaviours
{
    public class RequestPerformanceBehaviour<TRequest, TResponse>(
        ILogger<TRequest> logger,
        ICurrentUser currentUserService,
        IHttpContextAccessor httpContextAccessor)
        : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly Stopwatch _timer = new();

        // Prometheus Metrics
        private static readonly Counter SlowRequestCounter = Metrics.CreateCounter(
            "requests_slow_total",
            "Total number of requests that exceeded the performance threshold (500ms)",
            new CounterConfiguration
            {
                LabelNames = ["request_type", "method", "path", "organization_id"]
            });

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();

            var response = await next(cancellationToken);

            _timer.Stop();

            if (_timer.ElapsedMilliseconds > 500)
            {
                var name = typeof(TRequest).Name;
                var user = currentUserService.CurrentUser;
                var httpContext = httpContextAccessor.HttpContext;
                
                var userName = user?.PreferredUsername ?? "UNKNOWN";
                var organizationId = string.Empty;
                var method = httpContext?.Request.Method ?? "UNKNOWN";
                var path = httpContext?.Request?.Path.Value ?? "/";

                var userInfo = new
                {
                    userId = user?.Sub, 
                    userName = userName
                };

                var requestInfo = new
                {
                    method = method,
                    path = path,
                    correlationId = httpContext?.GetCorrelationId()
                };
                
                // Track slow request in Prometheus
                SlowRequestCounter.WithLabels(name, method, path, organizationId).Inc();

                logger.LogWarning(
                    "Long Running Request: {Name} ({ElapsedMilliseconds} ms) {@User} {@Request} {@Http}",
                    name,
                    _timer.ElapsedMilliseconds,
                    userInfo,
                    typeof(TRequest).Name,
                    requestInfo);
            }

            return response;
        }
    }
}