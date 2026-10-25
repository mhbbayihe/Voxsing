using VoxSign.Models;
using VoxSign.Shared;

namespace VoxSign.Services;

/// <summary>
/// Dictionnaire de signes illustrés, phrases courantes et saisie texte. Lit toujours la copie locale
/// (disponible hors ligne), rafraîchie depuis le serveur en arrière-plan.
/// </summary>
public interface IContentService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();

    Task<IReadOnlyList<Sign>> GetSignsAsync(string? categoryKey = null, string? search = null);

    Task<Sign?> GetSignAsync(string id);

    /// <summary>Nombre de repères connus (dictionnaire complet si l'appareil a déjà été en ligne).</summary>
    Task<int> GetCatalogSizeAsync();

    Task<int> GetOfflineSignCountAsync();

    Task<IReadOnlyList<ThemeDto>> GetThemesAsync();

    Task<IReadOnlyList<Phrase>> GetPhrasesAsync(string? themeKey = null, string? search = null);

    /// <summary>Phrases favorites, de la plus ancienne à la plus récente.</summary>
    Task<IReadOnlyList<Phrase>> GetFavoritePhrasesAsync();

    Task<IReadOnlyList<Sign>> GetFavoriteSignsAsync();

    /// <summary>Saisie texte → séquence de repères (serveur si joignable, sinon traduction sur l'appareil).</summary>
    Task<IReadOnlyList<Sign>> TranslateAsync(string text, CancellationToken ct = default);

    /// <summary>Traduction immédiate sur l'appareil, d'après le dictionnaire local (même algorithme que le serveur).</summary>
    Task<IReadOnlyList<Sign>> TranslateOnDeviceAsync(string text);

    /// <summary>Enregistre une phrase personnelle (sur l'appareil, puis sur le compte dès que possible).</summary>
    Task<Phrase> SaveCustomPhraseAsync(string text, string themeKey = "accueil");
}
