using Microsoft.Maui.Controls.Shapes;

namespace VoxSign.Controls;

/// <summary>Jauge d'espace utilisé (hauteur 6, rayon 3).</summary>
public class UsageBar : ContentView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(UsageBar), 0d, propertyChanged: (b, _, _) => ((UsageBar)b).UpdateFill());

    readonly Border _fill = new()
    {
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 3 },
        HorizontalOptions = LayoutOptions.Start,
    };

    public UsageBar()
    {
        HeightRequest = 6;
        _fill.Background = new SolidColorBrush((Color)Application.Current!.Resources["Primary"]);
        Content = new Grid
        {
            Children =
            {
                new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 3 },
                    Background = new SolidColorBrush((Color)Application.Current!.Resources["Line"]),
                },
                _fill,
            },
        };
        SizeChanged += (_, _) => UpdateFill();
    }

    /// <summary>Part remplie, de 0 à 1.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    void UpdateFill() => _fill.WidthRequest = Math.Clamp(Value, 0, 1) * Width;
}
