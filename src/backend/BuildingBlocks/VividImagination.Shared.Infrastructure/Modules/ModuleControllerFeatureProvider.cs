using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace VividImagination.Shared.Infrastructure.Modules;

/// <summary>
/// Restricts controller discovery to only the host application and registered/initialized modules.
/// Controllers from uninitialized or commented-out modules will not be discovered, mapped, or shown in Swagger.
/// </summary>
public class ModuleControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        if (!base.IsController(typeInfo))
        {
            return false;
        }

        // Host/entry assembly controllers are always permitted
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && typeInfo.Assembly == entryAssembly)
        {
            return true;
        }

        // Only permit controllers whose assembly belongs to a registered or initialized application module
        var registeredModules = ModuleRegistry.GetRegisteredModules();
        return registeredModules.Any(moduleType => moduleType.Assembly == typeInfo.Assembly)
            || AppModuleExtensions.InitializedModules.Any(moduleType => moduleType.Assembly == typeInfo.Assembly);
    }
}
