using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class SignDetailPage : ContentPage
{
    public SignDetailPage(SignDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
