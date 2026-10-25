namespace VoxSign.Controls;

/// <summary>
/// Interligne exprimé en points (comme dans la maquette) au lieu d'un multiplicateur.
/// Windows multiplie LineHeight par la taille de police, Android et iOS par la hauteur
/// naturelle de la ligne (ascendante + descendante d'Inter = 1,2109 em) : on convertit ici.
/// Le bloc de texte occupe alors (lignes × interligne), exactement comme un bloc CSS : dans les pages,
/// son haut se place à y0 + 0,6365 em − interligne / 2 (y0 = haut de la première ligne dans le PDF).
/// </summary>
public static class Typography
{
    const double InterNaturalLineHeight = 1.2109375;

    public static readonly BindableProperty LinePitchProperty = BindableProperty.CreateAttached(
        "LinePitch", typeof(double), typeof(Typography), -1d, propertyChanged: OnLinePitchChanged);

    public static double GetLinePitch(BindableObject view) => (double)view.GetValue(LinePitchProperty);

    public static void SetLinePitch(BindableObject view, double value) => view.SetValue(LinePitchProperty, value);

    static void OnLinePitchChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Label label)
            return;

        label.PropertyChanged -= OnLabelPropertyChanged;
        label.PropertyChanged += OnLabelPropertyChanged;
        Apply(label);
    }

    static void OnLabelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == Label.FontSizeProperty.PropertyName && sender is Label label)
            Apply(label);
    }

    static void Apply(Label label)
    {
        var pitch = GetLinePitch(label);
        if (pitch <= 0 || label.FontSize <= 0)
            return;

        var size = label.FontSize;
        var basis = DeviceInfo.Platform == DevicePlatform.WinUI ? size : size * InterNaturalLineHeight;
        label.LineHeight = pitch / basis;

        // Chaque plateforme place la première ligne différemment dans son interligne ; la maquette (CSS)
        // centre l'interligne autour du texte : ligne de base à P/2 + 0,3635 em du haut de la ligne.
        var cssBaseline = pitch / 2 + 0.3635 * size;
        var platformBaseline = DeviceInfo.Platform == DevicePlatform.WinUI
            ? 0.8 * pitch                          // BlockLineHeight : ligne de base à 80 % de l'interligne
            : DeviceInfo.Platform == DevicePlatform.Android
                ? InterAscent * size               // l'espace supplémentaire est ajouté sous la ligne
                : pitch - InterDescent * size;     // iOS : l'espace supplémentaire est ajouté au-dessus
        label.TranslationY = cssBaseline - platformBaseline;
    }

    const double InterAscent = 0.96875;
    const double InterDescent = 0.2421875;
}
