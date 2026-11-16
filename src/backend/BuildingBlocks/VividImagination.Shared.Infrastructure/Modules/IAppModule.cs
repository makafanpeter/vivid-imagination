using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VividImagination.Shared.Infrastructure.Modules;

/// <summary>
/// Defines the contract for an application feature module in the modular architecture.
/// </summary>
public interface IAppModule
{
    string ModuleName { get; }
    IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment);
    IMvcBuilder AddModule(IMvcBuilder builder);
    IApplicationBuilder UseModule(IApplicationBuilder app);
    IEndpointRouteBuilder UseModuleApi(IEndpointRouteBuilder app);
}

/// <summary>
/// Backwards-compatibility alias for <see cref="IAppModule"/>.
/// </summary>
[Obsolete("Use IAppModule instead.")]
public interface IAdinovaModule : IAppModule
{
}
