using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Shared.Infrastructure.Web.Authorization.Policies;

public static class PolicyServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services,
        Assembly assembly,
        string scope,
        IEnumerable<string> policies)
    {
        var policyNames = policies as IReadOnlyCollection<string> ?? policies.ToList();
        
        if (policyNames.Contains(scope))
        {
            throw new InvalidOperationException(
                $"Scope '{scope}' collides with a policy name in {nameof(policies)}. " +
                "Scope names and policy names must be distinct.");
        }
    
        services.Configure<AuthorizationOptions>(options =>
        {
            options.AddPolicy(scope, policy =>
                policy.RequireAssertion(context => context.User.HasScope(scope)));
            
            foreach (var policyName in policyNames)
            {
                options.AddPolicy(policyName, policy =>
                {
                    policy.AddRequirements(new PermissionRequirement
                    {
                        PermissionName = policyName,
                        Scope = scope
                    });
                });
            }
        });
    
        if (!services.Any(s => s.ServiceType == typeof(IAuthorizationHandler) &&
                                s.ImplementationType == typeof(PermissionRequirementHandler)))
        {
            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
        }
        
        var alreadyRegistered = services
            .Where(s => s.ServiceType == typeof(IAuthorizationHandler))
            .Select(s => s.ImplementationType)
            .ToHashSet();
    
        var requirementHandlerTypes = assembly.GetTypes()
            .Where(IsAuthorizationHandler)
            .Where(t => !alreadyRegistered.Contains(t));
    
        foreach (var type in requirementHandlerTypes)
        {
            services.AddSingleton(typeof(IAuthorizationHandler), type);
        }
    
        return services;
    }

    private static bool IsAuthorizationHandler(Type type)
    {
        if (type.BaseType == null)
        {
            return false;
        }

        if (!type.BaseType.IsGenericType)
        {
            return false;
        }

        var baseType = type.BaseType.GetGenericTypeDefinition();
        return baseType == typeof(AuthorizationHandler<>) || baseType == typeof(AuthorizationHandler<,>);
    }
}
