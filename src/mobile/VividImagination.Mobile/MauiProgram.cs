using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace VividImagination.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
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

            // Core Client Services
            builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
            builder.Services.AddTransient<AuthenticationHeaderHandler>();

            builder.Services.AddHttpClient<IAuthService, AuthService>()
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            builder.Services.AddHttpClient<IFeedService, FeedService>()
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            builder.Services.AddHttpClient<ISocialService, SocialService>()
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            builder.Services.AddHttpClient<IMediaService, MediaService>()
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

            return builder.Build();
        }
    }
}
