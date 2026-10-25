using System.Windows.Input;

namespace VoxSign.Controls;

public enum HeaderActionFrame
{
    None,
    Square,
    Rounded,
}

public partial class ScreenHeader : ContentView
{
    // Centre vertical commun des icônes de l'en-tête (y = 80,5 dans la maquette, en-tête à y = 61)
    const double IconCenterY = 19.5;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ScreenHeader));
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ScreenHeader));
    public static readonly BindableProperty TitleImageProperty = BindableProperty.Create(nameof(TitleImage), typeof(ImageSource), typeof(ScreenHeader));
    public static readonly BindableProperty ShowBackProperty = BindableProperty.Create(
        nameof(ShowBack), typeof(bool), typeof(ScreenHeader), true, propertyChanged: (b, _, _) => ((ScreenHeader)b).UpdateLayout());
    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(ScreenHeader));
    public static readonly BindableProperty ActionIconProperty = BindableProperty.Create(
        nameof(ActionIcon), typeof(string), typeof(ScreenHeader), propertyChanged: (b, _, _) => ((ScreenHeader)b).UpdateLayout());
    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(ScreenHeader), propertyChanged: (b, _, _) => ((ScreenHeader)b).OnPropertyChanged(nameof(HasAction)));
    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(ScreenHeader));
    public static readonly BindableProperty ActionFrameStyleProperty = BindableProperty.Create(
        nameof(ActionFrameStyle), typeof(HeaderActionFrame), typeof(ScreenHeader), HeaderActionFrame.None, propertyChanged: (b, _, _) => ((ScreenHeader)b).UpdateLayout());

    public ScreenHeader()
    {
        InitializeComponent();
        UpdateLayout();
    }

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public ImageSource? TitleImage { get => (ImageSource?)GetValue(TitleImageProperty); set => SetValue(TitleImageProperty, value); }
    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public ICommand? BackCommand { get => (ICommand?)GetValue(BackCommandProperty); set => SetValue(BackCommandProperty, value); }
    public string? ActionIcon { get => (string?)GetValue(ActionIconProperty); set => SetValue(ActionIconProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public HeaderActionFrame ActionFrameStyle { get => (HeaderActionFrame)GetValue(ActionFrameStyleProperty); set => SetValue(ActionFrameStyleProperty, value); }

    public bool HasAction => !string.IsNullOrEmpty(ActionText);

    void UpdateLayout()
    {
        if (TitleStack is null)
            return;

        // Avec Retour, le titre commence à x = 80 (bouton 23 → 69, puis 11 d'écart)
        TitleStack.Margin = new Thickness(ShowBack ? 11 : 0, 0, 0, 0);

        var framed = ActionFrameStyle != HeaderActionFrame.None;
        ActionFrame.StrokeThickness = framed ? 1 : 0;
        ActionFrame.Stroke = framed ? new SolidColorBrush((Color)Application.Current!.Resources["Line"]) : null;
        ActionFrame.Background = framed ? new SolidColorBrush((Color)Application.Current!.Resources["White"]) : Brush.Transparent;
        ActionFrame.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = ActionFrameStyle == HeaderActionFrame.Rounded ? 12.5 : 0,
        };

        // Le contour décale le contenu de 1 : on compense pour garder icône et libellé au même endroit
        var inset = framed ? 1 : 0;
        var iconHeight = ActionIcon is not null && Icons.All.TryGetValue(ActionIcon, out var icon) ? icon.Height : 0;
        ActionIconView.Margin = new Thickness(0, IconCenterY - iconHeight / 2 - inset, 0, 0);
        ActionLabel.Margin =new Thickness(0, 32.8 - inset, 0, 0);
    }
}
