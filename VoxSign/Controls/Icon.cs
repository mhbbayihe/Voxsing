using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace VoxSign.Controls;

/// <summary>
/// Icône vectorielle reprise telle quelle de la maquette (voir <see cref="Icons"/>).
/// Sa taille est celle du tracé d'origine : aucun redimensionnement, donc aucune perte de netteté.
/// </summary>
public class Icon : ContentView
{
    static readonly PathGeometryConverter GeometryConverter = new();

    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(Icon), null, propertyChanged: (b, _, _) => ((Icon)b).UpdateGeometry());

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(Icon), Colors.Black, propertyChanged: (b, _, n) => ((Icon)b)._path.Fill = new SolidColorBrush((Color)n));

    readonly Path _path = new() { Aspect = Stretch.None, Fill = new SolidColorBrush(Colors.Black) };

    public Icon()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
        InputTransparent = true;
        Content = _path;
    }

    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    void UpdateGeometry()
    {
        if (Name is null || !Icons.All.TryGetValue(Name, out var icon))
        {
            _path.Data = null;
            return;
        }

        _path.Data = (Geometry?)GeometryConverter.ConvertFromInvariantString(icon.Path);
        WidthRequest = icon.Width;
        HeightRequest = icon.Height;
    }
}
