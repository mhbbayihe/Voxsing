using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class TextToSignPage : ContentPage
{
    public TextToSignPage(TextToSignViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
