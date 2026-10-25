using System.Globalization;
using System.Text;

namespace VoxSign.Shared;

/// <summary>Comparaisons de texte insensibles à la casse et aux accents (« cafe » trouve « Café »).</summary>
public static class TextSearch
{
    /// <summary>Minuscules sans accents, apostrophes typographiques remplacées : « S’il Plaît » → « s'il plait ».</summary>
    public static string Fold(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool Contains(string text, string search) =>
        Fold(text).Contains(Fold(search.Trim()), StringComparison.Ordinal);
}
