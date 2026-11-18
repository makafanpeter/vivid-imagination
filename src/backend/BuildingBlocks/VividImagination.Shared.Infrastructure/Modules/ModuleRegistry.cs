namespace VividImagination.Shared.Infrastructure.Modules;

internal static class ModuleRegistry
{
    private static readonly HashSet<Type> RegisteredModules = [];

    public static void Register(Type moduleType)
    {
        if (!typeof(IAppModule).IsAssignableFrom(moduleType))
        {
            throw new ArgumentException(
                $"Type {moduleType.Name} is not a valid module. It must implement IAppModule."
            );
        }

        RegisteredModules.Add(moduleType);
    }

    public static void Register<T>() where T : IAppModule
    {
        RegisteredModules.Add(typeof(T));
    }

    public static bool IsRegistered(Type moduleType)
    {
        return RegisteredModules.Contains(moduleType);
    }

    public static bool IsRegistered<T>() where T : IAppModule
    {
        return RegisteredModules.Contains(typeof(T));
    }

    public static IEnumerable<Type> GetRegisteredModules()
    {
        return RegisteredModules;
    }

    public static void Clear()
    {
        RegisteredModules.Clear();
    }
}