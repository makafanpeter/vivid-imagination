using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using VividImagination.Modules.Identity.Persistence;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Domain;
using VividImagination.Shared.Infrastructure.Configurations;

namespace VividImagination.Modules.Identity.Services;

public static class IdentityServerExtensions
{
    
    public static IServiceCollection AddCustomIdentityServer(this IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        var applicationSettings = configuration
            .GetSection(nameof(ApplicationSettings))
            .Get<ApplicationSettings>() ?? throw new InvalidOperationException();
        
        var authProvider = applicationSettings.General.DefaultAuthProvider.ParseEnum<AuthProvider>();

        if (authProvider == AuthProvider.OpenIdConnect)
        {
            //services.AddScoped<CustomOpenIddictValidationHandlers>();
            var securitySettings = applicationSettings.SecuritySettings;

            services.AddIdentity<User, IdentityRole>(config =>
                {
                    config.Password.RequiredLength = securitySettings.PasswordMinimumLength;
                    config.Password.RequireDigit = securitySettings.PasswordRequireDigit;
                    config.Password.RequireNonAlphanumeric = securitySettings.PasswordRequireNonAlphanumeric;
                    config.Password.RequireUppercase = securitySettings.PasswordRequireUppercase;
                    config.Password.RequireLowercase = securitySettings.PasswordRequireLowercase;
                    config.User.RequireUniqueEmail = securitySettings.RequireUniqueEmail;
                    config.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(securitySettings.FailedPasswordLockoutMinutes);
                    config.Lockout.MaxFailedAccessAttempts = securitySettings.FailedPasswordAllowedAttempts;
                })
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();


            services.AddOpenIddict()
                // Register the OpenIddict core components
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<IdentityDbContext>();
                }).AddServer(options =>
                {
                    // Enable the token endpoint
                    options.SetTokenEndpointUris("/connect/token");

                    // Enable password + refresh (operator) and client_credentials (device) flows
                    options.AllowPasswordFlow()
                        .AllowRefreshTokenFlow()
                        .AllowClientCredentialsFlow();


                    // Token lifetimes
                    options.SetAccessTokenLifetime(TimeSpan.FromMinutes(applicationSettings.Jwt.AccessTokenLifetime));
                    options.SetRefreshTokenLifetime(TimeSpan.FromDays(applicationSettings.Jwt.RefreshTokenLifetime));
                    
                    // Register the signing and encryption credentials
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();

                    // Register the ASP.NET Core host and configure options
                    options.UseAspNetCore()
                        .EnableTokenEndpointPassthrough()
                        .DisableTransportSecurityRequirement(); // Only for development!

                    // Register scopes (permissions)
                    options.RegisterScopes(
                        [
                            OpenIddictConstants.Scopes.OpenId,
                            OpenIddictConstants.Scopes.Profile,
                            OpenIddictConstants.Scopes.Email,
                            OpenIddictConstants.Scopes.Roles,
                            OpenIddictConstants.Scopes.Phone,
                            OpenIddictConstants.Scopes.OfflineAccess,
                            Constants.StandardScopes.Permissions,
                            Constants.StandardScopes.RoleType,
                            Constants.StandardScopes.Role,
                            ..Constants.StandardScopes.GetApiScopes()  // includes terminal-api
                                
                        ]
                        
                    );
                    
                    
                })

                // Register the OpenIddict validation components
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseSystemNetHttp();
                    options.UseAspNetCore();
                    
                });
        }
        services.AddAuthorization();
        return services;
    }
}