using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class OfflinePage : ContentPage
{
    public OfflinePage(OfflineViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
