using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class DictionaryPage : ContentPage
{
    public DictionaryPage(DictionaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
