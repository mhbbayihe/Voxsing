using VoxSign.Services;
using VoxSign.Views;

namespace VoxSign;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.TextToSign, typeof(TextToSignPage));
        Routing.RegisterRoute(Routes.VoiceToText, typeof(VoiceToTextPage));
        Routing.RegisterRoute(Routes.Sequence, typeof(SequencePage));
        Routing.RegisterRoute(Routes.SignDetail, typeof(SignDetailPage));
        Routing.RegisterRoute(Routes.Phrases, typeof(PhrasesPage));
        Routing.RegisterRoute(Routes.Offline, typeof(OfflinePage));
    }
}
