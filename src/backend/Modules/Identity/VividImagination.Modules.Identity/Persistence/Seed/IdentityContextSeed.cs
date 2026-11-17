using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Infrastructure.Configurations;
using VividImagination.Shared.Infrastructure.IdsGenerator;
using VividImagination.Shared.Infrastructure.Persistence;
using VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

namespace VividImagination.Modules.Identity.Persistence.Seed;

internal class IdentityContextSeed(
    RoleManager<IdentityRole<long>> roleManager,
    IdentityDbContext identityContext, 
    IOpenIddictApplicationManager openIddictApplicationManager,
    IOptions<ApplicationSettings> applicationOptions, 
    StandardPermissionProvider standardPermissionProvider,
    ILogger<IdentityContextSeed> logger) :
    IDbSeeder<IdentityDbContext>
{
    private readonly ApplicationSettings _applicationSettings = applicationOptions.Value;
    public async Task SeedAsync(IdentityDbContext context)
    {
        if (await identityContext.Database.CanConnectAsync())
        {
            
            logger.LogInformation("Starting database seeding...");
            
            await CreateRolesIfNotExists();
            
            
            await SeedRolePermissions();
            
            await CreateIdentityClient();

            logger.LogInformation("Database seeding completed successfully");
        }

        
    }

    private async Task SeedRolePermissions()
    {
        var defaultRolePermissionProviders = standardPermissionProvider.Providers.ToList();

        logger.LogDebug("Seeding {ProviderCount} roles with providers", defaultRolePermissionProviders.Count);

        var rolesByName = (await identityContext.Roles.ToListAsync())
            .Where(r => !string.IsNullOrEmpty(r.Name))
            .ToDictionary(r => r.Name!, StringComparer.OrdinalIgnoreCase);

        var existingRoleClaims = await identityContext.RoleClaims
            .AsNoTracking()
            .Where(c => c.ClaimValue != null &&
                        (c.ClaimType == Constants.StandardScopes.Permissions ||
                         c.ClaimType == Constants.StandardScopes.Scopes))
            .ToListAsync();

        var permissionsByRoleId = existingRoleClaims
            .Where(c => c.ClaimType == Constants.StandardScopes.Permissions)
            .GroupBy(c => c.RoleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.ClaimValue!).ToHashSet(StringComparer.Ordinal));

        var scopesByRoleId = existingRoleClaims
            .Where(c => c.ClaimType == Constants.StandardScopes.Scopes)
            .GroupBy(c => c.RoleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.ClaimValue!).ToHashSet(StringComparer.Ordinal));

        var hasNewClaims = false;

        foreach (var permissionProvider in defaultRolePermissionProviders)
        {
            logger.LogDebug("Seeding Scope: {Scope}", permissionProvider.Scope);

            foreach (var defaultPermission in permissionProvider.GetDefaultRolePermissions())
            {
                logger.LogDebug("Seeding role: {Role}", defaultPermission.UserRoleSystemName);

                // 1. Ensure role exists
                if (!rolesByName.TryGetValue(defaultPermission.UserRoleSystemName, out var role))
                {
                    role = new IdentityRole<long>
                    {
                        Id = SnowFlakIdGenerator.NewId(),
                        Name = defaultPermission.UserRoleSystemName,
                        NormalizedName = defaultPermission.UserRoleSystemName.ToUpperInvariant()
                    };
                    await roleManager.CreateAsync(role);
                    rolesByName[defaultPermission.UserRoleSystemName] = role;
                    logger.LogDebug("Created role: {Role}", defaultPermission.UserRoleSystemName);
                }

                if (!permissionsByRoleId.TryGetValue(role.Id, out var existingPermissions))
                {
                    existingPermissions = new HashSet<string>(StringComparer.Ordinal);
                    permissionsByRoleId[role.Id] = existingPermissions;
                }

                // 2. Stage only new permission claims that don't exist
                foreach (var permissionRecord in defaultPermission.PermissionRecords)
                {
                    if (existingPermissions.Add(permissionRecord.PermissionName))
                    {
                        identityContext.RoleClaims.Add(new IdentityRoleClaim<long>
                        {
                            RoleId = role.Id,
                            ClaimType = Constants.StandardScopes.Permissions,
                            ClaimValue = permissionRecord.PermissionName
                        });
                        hasNewClaims = true;
                        logger.LogDebug("Added permission claim {Permission} to role {Role}", permissionRecord.PermissionName, defaultPermission.UserRoleSystemName);
                    }
                }

                if (!scopesByRoleId.TryGetValue(role.Id, out var existingScopes))
                {
                    existingScopes = new HashSet<string>(StringComparer.Ordinal);
                    scopesByRoleId[role.Id] = existingScopes;
                }

                // 3. Stage only new scope claims that don't exist
                if (existingScopes.Add(permissionProvider.Scope))
                {
                    identityContext.RoleClaims.Add(new IdentityRoleClaim<long>
                    {
                        RoleId = role.Id,
                        ClaimType = Constants.StandardScopes.Scopes,
                        ClaimValue = permissionProvider.Scope
                    });
                    hasNewClaims = true;
                    logger.LogDebug("Added scope claim {Scope} to role {Role}", permissionProvider.Scope, defaultPermission.UserRoleSystemName);
                }
            }
        }

        if (hasNewClaims)
        {
            await identityContext.SaveChangesAsync();
        }
    }


    private async Task CreateIdentityClient()
    {
       var authOptions = _applicationSettings.OpenIdConnectSettings;

       if (!string.IsNullOrEmpty(authOptions.DefaultClientId) && !string.IsNullOrEmpty(authOptions.DefaultClientSecret))
       {
           if (await openIddictApplicationManager.FindByClientIdAsync(authOptions.DefaultClientId) == null)
           {
               await openIddictApplicationManager.CreateAsync(new OpenIddictApplicationDescriptor
               {
                   ClientId = authOptions.DefaultClientId,
                   ClientSecret = authOptions.DefaultClientSecret, // Only needed if using RequireSecret()
                   DisplayName = $"{authOptions.DefaultClientId} Client",
                   Permissions =
                   {
                       OpenIddictConstants.Permissions.Endpoints.Token,
                       OpenIddictConstants.Permissions.GrantTypes.Password,
                       OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    
                       // Standard scopes
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Phone,
                    
                       // Your custom scopes from Constants
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.Role,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.RoleType,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.Permissions,
                        OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.FeedApi,
                        OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.MediaApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.ModerationApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.IdentityApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.SocialApi
                    
                   }
               });
           }


           var swaggerUi = "swagger-ui";
           if (await openIddictApplicationManager.FindByClientIdAsync(swaggerUi) == null)
           {
               await openIddictApplicationManager.CreateAsync(new OpenIddictApplicationDescriptor
               {
                   ClientId = swaggerUi,
                   ClientSecret = authOptions.DefaultClientSecret, // Optional, can be public client
                   DisplayName = "Swagger UI",
                
                   // Add your Swagger UI URLs
                   RedirectUris =
                   {
                       new Uri($"{authOptions.Authority}/swagger/oauth2-redirect.html")
                   },
                
                   Permissions =
                   {
                       // Endpoints
                       OpenIddictConstants.Permissions.Endpoints.Authorization,
                       OpenIddictConstants.Permissions.Endpoints.Token,
                    
                       // Grant types
                       OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                       OpenIddictConstants.Permissions.GrantTypes.Password,
                       OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    
                       // Response types
                       OpenIddictConstants.Permissions.ResponseTypes.Code,
                    
                       // Scopes
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                       OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.RoleType,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.Role,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.Permissions,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.IdentityApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.FeedApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.MediaApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.ModerationApi,
                       OpenIddictConstants.Permissions.Prefixes.Scope + Constants.StandardScopes.SocialApi
                   },
                
                   // Optional: Set token lifetimes
                   Settings =
                   {
                       [OpenIddictConstants.Settings.TokenLifetimes.AccessToken] = TimeSpan.FromHours(1).ToString(),
                       [OpenIddictConstants.Settings.TokenLifetimes.RefreshToken] = TimeSpan.FromDays(14).ToString()
                   }
               });
           }
       }
        
       
    }

    private async Task CreateRolesIfNotExists()
    {
        var defaultRoles = StandardPermissionProvider.GetDefaultRoles();
        var existingRoleNames = (await identityContext.Roles
                .AsNoTracking()
                .Select(r => r.Name)
                .ToListAsync())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var role in defaultRoles)
        {
            if (!existingRoleNames.Contains(role.UserRoleSystemName))
            {
                var toCreate = new IdentityRole<long>
                {
                    Id = SnowFlakIdGenerator.NewId(),
                    Name = role.UserRoleSystemName,
                    NormalizedName = role.UserRoleSystemName.ToUpperInvariant()
                };
                var result = await roleManager.CreateAsync(toCreate);
                if (result.Succeeded)
                {
                    existingRoleNames.Add(role.UserRoleSystemName);
                    logger.LogInformation("Successfully seeded role {Role}", role.UserRoleSystemName);
                }
                else
                {
                    logger.LogError("Failed to seed role {Role}: {Errors}", role.UserRoleSystemName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}