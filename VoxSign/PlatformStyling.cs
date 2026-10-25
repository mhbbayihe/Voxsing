using Microsoft.Maui.Handlers;

namespace VoxSign;

/// <summary>
/// Retire l'habillage natif des champs de saisie (bordure, fond, marges internes) :
/// la maquette dessine elle-même ses champs, le texte doit être posé au pixel près.
/// </summary>
public static class PlatformStyling
{
    public static void Configure()
    {
#if WINDOWS
        // Par défaut WinUI traite LineHeight comme un minimum : un interligne plus serré que
        // l'interligne naturel (titre 30/34) serait ignoré.
        LabelHandler.Mapper.AppendToMapping(nameof(Label.LineHeight), (handler, label) =>
        {
            if (label.LineHeight > 0)
                handler.PlatformView.LineStackingStrategy = Microsoft.UI.Xaml.LineStackingStrategy.BlockLineHeight;
        });

        // Sans cela, chaque texte est mesuré à la hauteur arrondie au pixel supérieur et les écarts
        // s'additionnent le long des piles verticales.
        LabelHandler.Mapper.AppendToMapping("VoxSignNoRounding", (handler, _) => handler.PlatformView.UseLayoutRounding = false);
#endif

        EntryHandler.Mapper.AppendToMapping("VoxSignBare", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
            StripTextBox(handler.PlatformView);
#endif
        });

        EditorHandler.Mapper.AppendToMapping("VoxSignBare", (handler, editor) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetPadding(0, 0, 0, 0);

            // Interligne de la maquette (Typography.LinePitch), ajouté sous chaque ligne comme pour les textes
            if (editor is BindableObject bindable && Controls.Typography.GetLinePitch(bindable) is > 0 and var pitch)
            {
                var density = handler.PlatformView.Resources?.DisplayMetrics?.Density ?? 1;
                var extra = (pitch - editor.Font.Size * 1.2109375) * density;
                handler.PlatformView.SetLineSpacing((float)Math.Max(0, extra), 1f);
            }
#elif IOS || MACCATALYST
            handler.PlatformView.TextContainerInset = UIKit.UIEdgeInsets.Zero;
            handler.PlatformView.TextContainer.LineFragmentPadding = 0;
#elif WINDOWS
            StripTextBox(handler.PlatformView);
#endif
        });
    }

#if WINDOWS
    static void StripTextBox(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        foreach (var key in new[]
                 {
                     "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused", "TextControlBackgroundDisabled",
                     "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled",
                 })
            textBox.Resources[key] = transparent;

        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Padding = new Microsoft.UI.Xaml.Thickness(0);
        textBox.MinHeight = 0;
        textBox.Background = transparent;
    }
#endif
}
