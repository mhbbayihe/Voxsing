namespace VoxSign;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Chaque écran recharge ses données quand il apparaît (retour arrière, changement d'onglet…)
        PageAppearing += (_, page) => (page.BindingContext as ViewModels.BaseViewModel)?.OnAppearing();
    }

    /// <summary>Mode capture (DEBUG) : enregistre chaque écran en PNG dans ce dossier puis quitte.</summary>
    public static string? CaptureDirectory { get; } = Environment.GetEnvironmentVariable("VOXSIGN_CAPTURE");

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
#if WINDOWS
        // Largeur utile de la maquette (420) + bordures de fenêtre
        window.Width = 434.7;
        window.Height = 900;
#endif
#if DEBUG
        if (CaptureDirectory is not null)
            window.Created += async (_, _) => await Diagnostics.DesignCapture.RunAsync(window, CaptureDirectory);
#endif
        return window;
    }
}
