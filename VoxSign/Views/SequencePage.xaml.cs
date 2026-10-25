using VoxSign.ViewModels;

namespace VoxSign.Views;

public partial class SequencePage : ContentPage
{
    public SequencePage(SequenceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
