using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using VoxSign.Models;

namespace VoxSign.Controls;

/// <summary>
/// Filtres segmentés sur 3 colonnes (cases de 44 bordées, écart 8, rangées espacées de 6).
/// Option choisie : fond et contour violets, texte blanc.
/// </summary>
public class SegmentedFilter : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SegmentedFilter), propertyChanged: (b, o, n) => ((SegmentedFilter)b).OnItemsSourceChanged(o, n));

    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(SegmentedFilter));

    readonly UniformGrid _grid = new() { Columns = 3, ColumnSpacing = 8, RowSpacing = 6 };

    public SegmentedFilter() => Content = _grid;

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Reçoit le <see cref="FilterOption"/> touché.</summary>
    public ICommand? SelectCommand { get => (ICommand?)GetValue(SelectCommandProperty); set => SetValue(SelectCommandProperty, value); }

    static Color Res(string key) => (Color)Application.Current!.Resources[key];

    void OnItemsSourceChanged(object? oldValue, object? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += OnCollectionChanged;
        Rebuild();
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    void Rebuild()
    {
        _grid.Clear();
        foreach (var option in ItemsSource?.OfType<FilterOption>() ?? [])
            _grid.Add(BuildSegment(option));
    }

    View BuildSegment(FilterOption option)
    {
        var label = new Label
        {
            Text = option.Label,
            FontFamily = "InterSemiBold",
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.NoWrap,
        };
        var segment = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12.5 },
            HeightRequest = 46,
            // Contour de 1 à l'extérieur de la case, comme dans la maquette
            Margin = new Thickness(-1, 0),
            Content = label,
        };
        SemanticProperties.SetDescription(segment, option.Label);

        void Apply()
        {
            segment.Background = new SolidColorBrush(option.IsSelected ? Res("Primary") : Res("White"));
            segment.Stroke = new SolidColorBrush(option.IsSelected ? Res("Primary") : Res("Line"));
            label.TextColor = option.IsSelected ? Res("White") : Res("Muted");
        }

        Apply();
        option.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterOption.IsSelected))
                Apply();
        };
        segment.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                if (SelectCommand?.CanExecute(option) == true)
                    SelectCommand.Execute(option);
            }),
        });
        return segment;
    }
}
