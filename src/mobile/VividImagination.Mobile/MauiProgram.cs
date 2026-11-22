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
            var memoryStreams = new List<MemoryStream>();

            void AddJsonAsset(string filename)
            {
                try
                {
                    var assembly = typeof(MauiProgram).Assembly;
                    var resourceName = assembly.GetManifestResourceNames()
                        .SingleOrDefault(name => name.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

                    if (resourceName is null)
                    {
                        throw new InvalidOperationException($"Embedded app configuration resource '{filename}' was not found.");
                    }

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream is null)
                    {
                        throw new InvalidOperationException($"Embedded app configuration resource '{filename}' could not be opened.");
                    }

                    var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    memoryStreams.Add(memoryStream);
                    configBuilder.AddJsonStream(memoryStream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load {filename}: {ex.Message}");
                }
            }

            AddJsonAsset("appsettings.json");

#if DEBUG
            const string environment = "Development";
#else
            const string environment = "Production";
#endif

            AddJsonAsset($"appsettings.{environment}.json");

            var configuration = configBuilder.Build();
            builder.Configuration.AddConfiguration(configuration);

            foreach (var stream in memoryStreams)
            {
                stream.Dispose();
            }

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
                    fonts.AddFont("Syne-Bold.ttf", "SyneBold");
                    fonts.AddFont("Manrope-Regular.ttf", "ManropeRegular");
                    fonts.AddFont("Manrope-SemiBold.ttf", "ManropeSemiBold");
                    fonts.AddFont("Manrope-SemiBold.ttf", "ManropeBold");
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

            // Authentication Service (Registered as Singleton so all ViewModels and Pages share authentication state)
            builder.Services.AddHttpClient("AuthService", (sp, client) =>
            {
                var config = sp.GetRequiredService<IConfigurationService>();
                client.Timeout = TimeSpan.FromSeconds(config.Options.Api.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DynamicBaseAddressHandler>()
            .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            builder.Services.AddSingleton<IAuthService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("AuthService");
                var tokenStorage = sp.GetRequiredService<ITokenStorageService>();
                var configService = sp.GetRequiredService<IConfigurationService>();
                var apiClient = sp.GetRequiredService<IVividImaginationClient>();
                return new AuthService(httpClient, tokenStorage, configService, apiClient);
            });

            // Photo Loader & Cache Service
            builder.Services.AddSingleton<IPhotoLoaderService, PhotoLoaderService>();

            // ViewModels
            builder.Services.AddSingleton<IToastService>(ToastService.Current);
            builder.Services.AddSingleton<FeedViewModel>();
            builder.Services.AddTransient<PhotoUploadViewModel>();
            builder.Services.AddSingleton<ProfileViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ReviewQueueViewModel>();

            // Pages & Shell
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<FeedPage>();
            builder.Services.AddTransient<PhotoUploadPage>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ReviewQueuePage>();

            var app = builder.Build();
            AppConfig.Service = app.Services.GetRequiredService<IConfigurationService>();
            return app;
        }
    }
}
