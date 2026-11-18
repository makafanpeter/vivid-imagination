namespace VividImagination.Shared.Infrastructure.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    public Type[] Dependencies { get; }

    public DependsOnAttribute(params Type[] dependencies)
    {
        Dependencies = dependencies;
    }
}