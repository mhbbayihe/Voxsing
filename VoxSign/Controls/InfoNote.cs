using Microsoft.Maui.Controls.Shapes;

namespace VoxSign.Controls;

public enum InfoNoteVariant
{
    /// <summary>Fond teinté, texte encre.</summary>
    Tint,
    /// <summary>Fond blanc bordé, texte violet.</summary>
    Outline,
    /// <summary>Fond blanc bordé, texte gris.</summary>
    OutlineMuted,
    /// <summary>Fond vert pâle, texte et icône verts.</summary>
    Success,
}

/// <summary>Encart d'information (icône « i » à 13,13 ; texte 12/17 à 40 du bord gauche).</summary>
public class InfoNote : ContentView
{
    public const string LsfDisclaimer =
        "LSF · Illustrations indicatives, à faire valider par une personne compétente. Ce support n’est pas une traduction certifiée.";

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(InfoNote), LsfDisclaimer, propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    public static readonly BindableProperty VariantProperty = BindableProperty.Create(
        nameof(Variant), typeof(InfoNoteVariant), typeof(InfoNote), InfoNoteVariant.Tint, propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(InfoNote), "Info", propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    public static readonly BindableProperty TextLeftProperty = BindableProperty.Create(
        nameof(TextLeft), typeof(double), typeof(InfoNote), 40d, propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    /// <summary>Marge haute de l'icône et de la première ligne (13 ou 15 selon les écrans).</summary>
    public static readonly BindableProperty InsetProperty = BindableProperty.Create(
        nameof(Inset), typeof(double), typeof(InfoNote), 13d, propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    /// <summary>Position horizontale de l'icône ; par défaut 27 avant le texte.</summary>
    public static readonly BindableProperty IconLeftProperty = BindableProperty.Create(
        nameof(IconLeft), typeof(double), typeof(InfoNote), -1d, propertyChanged: (b, _, _) => ((InfoNote)b).Build());

    public InfoNote() => Build();

    public double Inset { get => (double)GetValue(InsetProperty); set => SetValue(InsetProperty, value); }
    public double IconLeft { get => (double)GetValue(IconLeftProperty); set => SetValue(IconLeftProperty, value); }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public InfoNoteVariant Variant { get => (InfoNoteVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public double TextLeft { get => (double)GetValue(TextLeftProperty); set => SetValue(TextLeftProperty, value); }

    static Color Res(string key) => (Color)Application.Current!.Resources[key];

    void Build()
    {
        var outlined = Variant is InfoNoteVariant.Outline or InfoNoteVariant.OutlineMuted;
        var (background, textColor, iconColor) = Variant switch
        {
            InfoNoteVariant.Outline => (Res("White"), Res("Primary"), Res("Primary")),
            InfoNoteVariant.OutlineMuted => (Res("White"), Res("Muted"), Res("Primary")),
            InfoNoteVariant.Success => (Res("SuccessTint"), Res("Success"), Res("Success")),
            _ => (Res("Tint"), Res("Ink"), Res("Primary")),
        };

        var text = new Label
        {
            Text = Text,
            FontSize = 12,
            TextColor = textColor,
            // Ligne de 17 : la première ligne est remontée de la demi-interligne (0,86)
            Margin = new Thickness(TextLeft, Inset - 0.86, 16, Inset - 1.14),
        };
        Typography.SetLinePitch(text, 17);

        Content = new Border
        {
            Background = new SolidColorBrush(background),
            Stroke = outlined ? new SolidColorBrush(Res("Line")) : null,
            StrokeThickness = outlined ? 1 : 0,
            StrokeShape = new RoundRectangle { CornerRadius = outlined ? 12.5 : 12 },
            Margin = outlined ? new Thickness(-1, 0) : new Thickness(0),
            Content = new Grid
            {
                Children =
                {
                    new Icon { Name = Icon, Color = iconColor, Margin = new Thickness(IconLeft >= 0 ? IconLeft : TextLeft - 27, Inset, 0, 0) },
                    text,
                },
            },
        };
    }
}
