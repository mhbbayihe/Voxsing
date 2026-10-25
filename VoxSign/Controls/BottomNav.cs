using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace VoxSign.Controls;

public enum NavTab
{
    Accueil,
    Dictionnaire,
    Favoris,
    Echanges,
}

/// <summary>Les variantes visuelles de la barre, telles qu'elles apparaissent selon les écrans de la maquette.</summary>
public enum NavVariant
{
    /// <summary>Onglet actif teinté, les autres sans fond (Accueil et ses sous-écrans).</summary>
    Tinted,
    /// <summary>Fond de barre gris clair, onglet actif cerclé de violet (Dictionnaire, Favoris).</summary>
    Outlined,
    /// <summary>Onglet actif cerclé de gris (fiche d'un signe).</summary>
    OutlinedNeutral,
    /// <summary>Tous les onglets cerclés de gris (Sans connexion).</summary>
    Boxed,
    /// <summary>Tous les onglets cerclés de gris, fond de barre gris clair (Échanges).</summary>
    BoxedMuted,
}

/// <summary>Barre de navigation basse : filet de 1, puis 96 de haut, onglets de 51 à 10,5 du haut.</summary>
public class BottomNav : ContentView
{
    static readonly (NavTab Tab, string Icon, string Text, double IconTop)[] Items =
    [
        (NavTab.Accueil, "Home", "Accueil", 7.3),
        (NavTab.Dictionnaire, "Book", "Dictionnaire", 8.3),
        (NavTab.Favoris, "Heart", "Favoris", 9.0),
        (NavTab.Echanges, "Messages", "Échanges", 7.3),
    ];

    public static readonly BindableProperty ActiveProperty = BindableProperty.Create(
        nameof(Active), typeof(NavTab), typeof(BottomNav), NavTab.Accueil, propertyChanged: (b, _, _) => ((BottomNav)b).Build());

    public static readonly BindableProperty VariantProperty = BindableProperty.Create(
        nameof(Variant), typeof(NavVariant), typeof(BottomNav), NavVariant.Tinted, propertyChanged: (b, _, _) => ((BottomNav)b).Build());

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(BottomNav));

    public BottomNav() => Build();

    public NavTab Active { get => (NavTab)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public NavVariant Variant { get => (NavVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Reçoit l'onglet touché (<see cref="NavTab"/>) en paramètre.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    static Color Res(string key) => (Color)Application.Current!.Resources[key];

    void Build()
    {
        var mutedBar = Variant is NavVariant.Outlined or NavVariant.BoxedMuted;
        var tabs = new Grid
        {
            ColumnSpacing = 4,
            Padding = new Thickness(14, 10.5, 14, 0),
            HeightRequest = 96,
            BackgroundColor = mutedBar ? Res("NavMuted") : Res("White"),
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)],
            RowDefinitions = [new(51), new(GridLength.Star)],
        };

        for (var i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            tabs.Add(BuildTab(item, item.Tab == Active), i, 0);
        }

        Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                new BoxView { HeightRequest = 1, Color = Res("Line") },
                tabs,
            },
        };
    }

    View BuildTab((NavTab Tab, string Icon, string Text, double IconTop) item, bool active)
    {
        Color? stroke = (Variant, active) switch
        {
            (NavVariant.Outlined, true) => Res("Primary"),
            (NavVariant.OutlinedNeutral, true) => Res("Line"),
            (NavVariant.Boxed or NavVariant.BoxedMuted, _) => Res("Line"),
            _ => null,
        };
        var fill = Variant == NavVariant.Tinted ? (active ? Res("Tint") : Colors.Transparent) : Res("White");
        var color = active ? Res("Primary") : Res("Muted");
        var inset = stroke is null ? 0 : 1;

        var tab = new Border
        {
            Background = new SolidColorBrush(fill),
            Stroke = stroke is null ? null : new SolidColorBrush(stroke),
            StrokeThickness = inset,
            StrokeShape = new RoundRectangle { CornerRadius = 12 + inset / 2.0 },
            // Le contour de 1 est dessiné à l'extérieur de l'onglet, comme dans la maquette
            Margin = new Thickness(-inset),
            Content = new Grid
            {
                Children =
                {
                    new Icon { Name = item.Icon, Color = color, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, item.IconTop, 0, 0) },
                    new Label
                    {
                        Text = item.Text,
                        FontFamily = active ? "InterBold" : "InterMedium",
                        FontSize = 11,
                        TextColor = color,
                        HorizontalOptions = LayoutOptions.Center,
                        LineBreakMode = LineBreakMode.NoWrap,
                        Margin = new Thickness(0, 32.3, 0, 0),
                    },
                },
            },
        };
        SemanticProperties.SetDescription(tab, item.Text);
        tab.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                if (Command?.CanExecute(item.Tab) == true)
                    Command.Execute(item.Tab);
            }),
        });
        return tab;
    }
}
