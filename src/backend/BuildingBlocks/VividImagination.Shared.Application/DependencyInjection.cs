using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using VividImagination.Shared.Application.Mapping;

namespace VividImagination.Shared.Application;

public static class DependencyInjection
{
    
    public static IServiceCollection AddCustomAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        var allAssemblies = assemblies
            .Concat([typeof(DependencyInjection).Assembly])
            .Distinct()
            .ToArray();

        var profileConfiguration = CreateMapperProfileConfiguration(allAssemblies);
        var mapperConfiguration = new MapperConfiguration(profileConfiguration);
    
        AutoMapperConfiguration.Init(mapperConfiguration);
        services.AddAutoMapper(profileConfiguration, allAssemblies);
        return services;
    }

    private static Action<IMapperConfigurationExpression> CreateMapperProfileConfiguration(Assembly[] assemblies)
    {
        return cfg =>
        {
            cfg.AddProfile<ExceptionMappingProfile>();
            cfg.AddProfile(new MappingProfile(assemblies));
        };
    }
    
}