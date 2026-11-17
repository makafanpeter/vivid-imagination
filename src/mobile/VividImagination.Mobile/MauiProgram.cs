using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VividImagination.Mobile.Services.Configuration;

namespace VividImagination.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // 1. Build Hierarchical Configuration from Embedded/Raw Assets
            var configBuilder = new ConfigurationBuilder();
            try
            {
                using var baseStream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
                configBuilder.AddJsonStream(baseStream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load appsettings.json: {ex.Message}");
            }

#if DEBUG
            const string environment = "Development";
#else
            const string environment = "Production";
#endif

            try
            {
                using var envStream = FileSystem.OpenAppPackageFileAsync($"appsettings.{environment}.json").GetAwaiter().GetResult();
                configBuilder.AddJsonStream(envStream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load appsettings.{environment}.json: {ex.Message}");
            }

            var configuration = configBuilder.Build();
            builder.Configuration.AddConfiguration(configuration);

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
                    Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
                    {
                        handler.PlatformView.SingleSelectionFollowsFocus = false;
                    });
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Options & Configuration Service
            builder.Services.Configure<AppConfigOptions>(configuration.GetSection(AppConfigOptions.SectionName));
            builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Security & HTTP Handlers
            builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
            builder.Services.AddTransient<DynamicBaseAddressHandler>();
            builder.Services.AddTransient<AuthenticationHeaderHandler>();

            // Strongly-Typed OpenAPI Client
            builder.Services.AddHttpClient<IVividImaginationClient, VividImaginationClient>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfigurationService>();
                client.BaseAddress = new Uri(config.ActiveBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.Options.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DynamicBaseAddressHandler>()
            .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            // Authentication Service
            builder.Services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfigurationService>();
                client.Timeout = TimeSpan.FromSeconds(config.Options.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DynamicBaseAddressHandler>()
            .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            // ViewModels
            builder.Services.AddSingleton<FeedViewModel>();
            builder.Services.AddTransient<PhotoUploadViewModel>();
            builder.Services.AddSingleton<ProfileViewModel>();
            builder.Services.AddTransient<LoginViewModel>();

            // Pages
            builder.Services.AddSingleton<FeedPage>();
            builder.Services.AddTransient<PhotoUploadPage>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddTransient<LoginPage>();

            var app = builder.Build();
            AppConfig.Service = app.Services.GetRequiredService<IConfigurationService>();
            return app;
        }
    }
}
