using Microsoft.Extensions.Logging;
using VoxSign.Services;
using VoxSign.Services.Api;
using VoxSign.Services.Local;
using VoxSign.ViewModels;
using VoxSign.Views;

namespace VoxSign;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        PlatformStyling.Configure();

        // Plateforme
        builder.Services.AddSingleton(TextToSpeech.Default);
        builder.Services.AddSingleton(Clipboard.Default);
        builder.Services.AddSingleton(Share.Default);
        builder.Services.AddSingleton(Connectivity.Current);
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton(Preferences.Default);

        // Serveur VoxSign
        builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(ApiSettings.BaseUrl), Timeout = TimeSpan.FromSeconds(15) });
        builder.Services.AddSingleton<ISessionStore, SessionStore>();
        builder.Services.AddSingleton<IVoxSignApi, VoxSignApi>();

        // Données sur l'appareil (hors ligne) et synchronisation
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<CatalogCache>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
        builder.Services.AddSingleton<IContentService, ContentService>();
        builder.Services.AddSingleton<IOfflinePackService, OfflinePackService>();

        // Fonctionnalités
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ISpeechService, SpeechService>();
        builder.Services.AddSingleton<ISpeechToTextService, SimulatedSpeechToTextService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, ShellDialogService>();

        // Écrans racines (conservés) et écrans empilés (recréés à chaque ouverture)
        AddRoot<LoginPage, LoginViewModel>(builder.Services);
        AddRoot<HomePage, HomeViewModel>(builder.Services);
        AddRoot<DictionaryPage, DictionaryViewModel>(builder.Services);
        AddRoot<FavoritesPage, FavoritesViewModel>(builder.Services);
        AddRoot<ChatPage, ChatViewModel>(builder.Services);
        AddDetail<RegisterPage, RegisterViewModel>(builder.Services);
        AddDetail<TextToSignPage, TextToSignViewModel>(builder.Services);
        AddDetail<VoiceToTextPage, VoiceToTextViewModel>(builder.Services);
        AddDetail<SequencePage, SequenceViewModel>(builder.Services);
        AddDetail<SignDetailPage, SignDetailViewModel>(builder.Services);
        AddDetail<PhrasesPage, PhrasesViewModel>(builder.Services);
        AddDetail<OfflinePage, OfflineViewModel>(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    static void AddRoot<TPage, TViewModel>(IServiceCollection services)
        where TPage : class
        where TViewModel : class
    {
        services.AddSingleton<TPage>();
        services.AddSingleton<TViewModel>();
    }

    static void AddDetail<TPage, TViewModel>(IServiceCollection services)
        where TPage : class
        where TViewModel : class
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();
    }
}
