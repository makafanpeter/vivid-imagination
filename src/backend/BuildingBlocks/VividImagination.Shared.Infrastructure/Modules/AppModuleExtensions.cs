using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Shared.Infrastructure.Modules;

/// <summary>
/// Provides extension methods for registering, configuring, and mapping application modules.
/// </summary>
public static class AppModuleExtensions
{
    public static readonly HashSet<Type> InitializedModules = [];
    private static readonly List<IAppModule> _initializedInstances = [];

    /// <summary>
    /// Gets all instantiated and initialized application module instances.
    /// </summary>
    public static IReadOnlyList<IAppModule> GetInitializedModuleInstances()
    {
        lock (_initializedInstances)
        {
            return _initializedInstances.ToList();
        }
    }

    /// <summary>
    /// Discovers and registers all application modules from the specified assembly.
    /// </summary>
    public static IServiceCollection AddModulesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var moduleTypes = assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false }
                && typeof(IAppModule).IsAssignableFrom(t)
            );

        foreach (var type in moduleTypes)
        {
            RegisterModule(services, type);
        }

        return services;
    }

    /// <summary>
    /// Registers the specified application modules.
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        params Type[] moduleTypes)
    {
        foreach (var type in moduleTypes)
        {
            RegisterModule(services, type);
        }

        return services;
    }

    /// <summary>
    /// Restricts controller discovery to only the host application and registered/initialized modules,
    /// and adds registered module application parts to MVC.
    /// </summary>
    public static IMvcBuilder AddRegisteredModulesToMvc(this IMvcBuilder builder)
    {
        builder.ConfigureApplicationPartManager(manager =>
        {
            var defaultProvider = manager.FeatureProviders.OfType<ControllerFeatureProvider>().FirstOrDefault();
            if (defaultProvider != null)
            {
                manager.FeatureProviders.Remove(defaultProvider);
            }
            manager.FeatureProviders.Add(new ModuleControllerFeatureProvider());
        });

        var moduleTypes = ModuleRegistry.GetRegisteredModules();
        foreach (var moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is IAppModule module)
            {
                builder = module.AddModule(builder);
            }
        }

        return builder;
    }

    /// <summary>
    /// Applies middleware from all registered application modules.
    /// </summary>
    public static IApplicationBuilder UseModules(this IApplicationBuilder app)
    {
        var modules = InitializedModules
            .Select(type => app.ApplicationServices.GetService(type) as IAppModule)
            .Where(m => m != null)
            .ToList();

        foreach (var module in modules!)
        {
            Console.WriteLine($"🔧 Applying middleware for {module.ModuleName}");
            app = module.UseModule(app);
        }

        return app;
    }

    /// <summary>
    /// Maps endpoints for all registered application modules.
    /// </summary>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var modules = InitializedModules
            .Select(type => endpoints.ServiceProvider.GetService(type) as IAppModule)
            .Where(m => m != null)
            .ToList();

        foreach (var module in modules!)
        {
            Console.WriteLine($"🗺️  Mapping endpoints for {module.ModuleName}");
            endpoints = module.UseModuleApi(endpoints);
        }

        return endpoints;
    }

    /// <summary>
    /// Registers a single module, initializes it, and adds it to the dependency injection container.
    /// </summary>
    private static void RegisterModule(IServiceCollection services, Type module)
    {
        ModuleRegistry.Register(module);

        var modules = ModuleRegistry
            .GetRegisteredModules()
            .Select(type => GetInstance(type, services))
            .ToList();

        foreach (var m in modules)
        {
            InitializeModule(m, services);
        }
    }

    private static IAppModule GetInstance(Type type, IServiceCollection services)
    {
        var isConstructorInfo = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length == 0);

        if (isConstructorInfo is null)
            return (IAppModule)Activator.CreateInstance(type)!;

        var sp = services.BuildServiceProvider();
        return (IAppModule)ActivatorUtilities.CreateInstance(sp, type);
    }

    private static void InitializeModule(IAppModule module, IServiceCollection services)
    {
        var moduleType = module.GetType();

        if (InitializedModules.Contains(moduleType))
        {
            return;
        }

        Console.WriteLine($"🔍 Discovering dependencies for {module.ModuleName}...");

        var dependsOnAttributes = moduleType
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>();

        if (!dependsOnAttributes.Any())
        {
            Console.WriteLine($"📦 No dependencies found for {module.ModuleName}.");
        }

        foreach (var attribute in dependsOnAttributes)
        {
            Console.WriteLine($"📦 Total {attribute.Dependencies.Length} dependencies found for {module.ModuleName}.");

            foreach (var dependency in attribute.Dependencies)
            {
                if (!InitializedModules.Contains(dependency))
                {
                    Console.WriteLine($"🔗 {module.ModuleName} depends on {dependency.Name}. Initializing dependency... ⚙️");

                    var dependencyModule = GetInstance(dependency, services);
                    ModuleRegistry.Register(dependency);
                    InitializeModule(dependencyModule, services);
                }
            }
        }

        var sp = services.BuildServiceProvider();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var environment = sp.GetRequiredService<IHostEnvironment>();

        module.AddModule(services, configuration, environment);

        InitializedModules.Add(moduleType);
        lock (_initializedInstances)
        {
            if (!_initializedInstances.Any(i => i.GetType() == moduleType))
            {
                _initializedInstances.Add(module);
            }
        }
        services.AddSingleton(moduleType, module);

        Console.WriteLine($"🚀 {module.ModuleName} initialized.");
    }

    public static IServiceCollection AddPermissionProviders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var providers = assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(t =>
                t is { IsAbstract: false, IsInterface: false } &&
                typeof(IPermissionProvider).IsAssignableFrom(t))
            .Select(t => t.AsType());

        foreach (var provider in providers)
        {
            services.AddScoped(typeof(IPermissionProvider), provider);
        }

        return services;
    }

    /// <summary>
    /// Resets initialized and registered modules state (essential for test process isolation).
    /// </summary>
    public static void Reset()
    {
        InitializedModules.Clear();
        lock (_initializedInstances)
        {
            _initializedInstances.Clear();
        }
        ModuleRegistry.Clear();
    }
}
