namespace VoxSign.Shared;

// Contrats d'échange entre l'API et l'application (JSON, camelCase).

public record CategoryDto(string Key, string Label, string ShortLabel, int SignCount);

/// <param name="ImageKey">Clé d'image (« merci ») : l'application cherche d'abord le fichier téléchargé, puis l'image embarquée, puis <paramref name="ImageUrl"/>.</param>
public record SignDto(
    string Id,
    string Word,
    string CategoryKey,
    string Category,
    string ImageKey,
    string ImageUrl,
    string Usage,
    string UsageDescription,
    string Example,
    IReadOnlyList<string> Keywords);

public record ThemeDto(string Key, string Label, string Title, string Description, int PhraseCount);

/// <param name="Context">Contexte affiché dans les favoris (« Me présenter ») ; à défaut, le thème.</param>
public record PhraseDto(string Id, string Text, string ThemeKey, string Theme, string Intent, string? Context, bool IsCustom);

public record PackSummaryDto(string Id, string Name, string Icon, int SignCount, int PhraseCount, long SizeBytes, int Version);

/// <summary>Contenu complet d'un pack, à stocker sur l'appareil pour un usage hors ligne.</summary>
public record PackContentDto(PackSummaryDto Pack, IReadOnlyList<SignDto> Signs, IReadOnlyList<PhraseDto> Phrases);

public record TranslateRequest(string Text);

/// <param name="SignId">Repère correspondant, ou <c>null</c> si le mot n'a pas d'illustration.</param>
public record TranslationTokenDto(string Text, string? SignId);

public record TranslationDto(string Text, IReadOnlyList<TranslationTokenDto> Tokens, IReadOnlyList<SignDto> Signs);

public record RegisterRequest(string FirstName, string Email, string Password, string SignLanguage);

public record LoginRequest(string Email, string Password);

public record SpeechPreferencesDto(string Locale, float Pitch, float Volume)
{
    public static SpeechPreferencesDto Default { get; } = new("fr-FR", 1f, 1f);
}

public record UserDto(Guid Id, string FirstName, string Email, string SignLanguage, SpeechPreferencesDto Speech);

public record UpdatePreferencesRequest(string? SignLanguage, SpeechPreferencesDto? Speech);

public record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);

public record FavoritesDto(IReadOnlyList<string> SignIds, IReadOnlyList<string> PhraseIds);

/// <summary>Création ou mise à jour d'une phrase personnelle (identifiant choisi par l'appareil, pour fonctionner hors ligne).</summary>
public record CustomPhraseRequest(string Text, string? ThemeKey, string? Intent);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total);

public record ProblemDto(string Title, string? Detail);
