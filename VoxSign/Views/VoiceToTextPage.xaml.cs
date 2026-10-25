using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class VoiceToTextPage : ContentPage
{
    readonly VoiceToTextViewModel _viewModel;

    public VoiceToTextPage(VoiceToTextViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if DEBUG
        // En recette visuelle, l'écran reste dans l'état illustré par la maquette
        if (App.CaptureDirectory is not null)
            return;
#endif
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopAsync();
    }
}
