using System.Reflection;
using AutoMapper;

namespace VividImagination.Shared.Application.Mapping;

public class MappingProfile : Profile
{
    /// <summary>
    /// Scan the executing assembly only (classic monolith scenario).
    /// </summary>
    public MappingProfile() : this(Assembly.GetExecutingAssembly())
    { }

    /// <summary>
    /// Scan one or more assemblies (modular / plug‑in scenario).
    /// </summary>
    /// <param name="assemblies">
    /// Assemblies that contain types implementing <c>IMapFrom&lt;&gt;</c>.
    /// </param>
    public MappingProfile(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be supplied.", nameof(assemblies));

        foreach (var asm in assemblies.Distinct())          // avoid duplicates
            ApplyMappingsFromAssembly(asm);
    }

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        var types = assembly.GetExportedTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            .ToList();

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod("Mapping")
                             ?? type.GetInterface("IMapFrom`1").GetMethod("Mapping");

            methodInfo?.Invoke(instance, new object[] { this });

        }
    }



}