using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class PhrasesPage : ContentPage
{
    public PhrasesPage(PhrasesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
