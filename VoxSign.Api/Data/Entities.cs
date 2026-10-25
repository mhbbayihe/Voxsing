namespace VoxSign.Api.Data;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FirstName { get; set; }
    /// <summary>Adresse e-mail normalisée (minuscules, sans espaces) : identifiant de connexion.</summary>
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string SignLanguage { get; set; } = "LSF";
    public string SpeechLocale { get; set; } = "fr-FR";
    public float SpeechPitch { get; set; } = 1f;
    public float SpeechVolume { get; set; } = 1f;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<FavoriteSign> FavoriteSigns { get; set; } = [];
    public List<FavoritePhrase> FavoritePhrases { get; set; } = [];
}

public class Category
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string ShortLabel { get; set; }
    public int Order { get; set; }
}

public class Sign
{
    public required string Id { get; set; }
    public required string Word { get; set; }
    public required string CategoryKey { get; set; }
    public Category? Category { get; set; }
    public required string ImageKey { get; set; }
    public string Usage { get; set; } = string.Empty;
    public string UsageDescription { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    /// <summary>Mots et expressions qui renvoient à ce repère.</summary>
    public List<string> Keywords { get; set; } = [];
}

public class Theme
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int Order { get; set; }
}

public class Phrase
{
    public required string Id { get; set; }
    public required string Text { get; set; }
    public required string ThemeKey { get; set; }
    public Theme? Theme { get; set; }
    public required string Intent { get; set; }
    public string? Context { get; set; }
    /// <summary>Phrase personnelle (« Créer ma propre phrase ») : visible de son seul auteur.</summary>
    public Guid? OwnerId { get; set; }
    public int Order { get; set; }
}

public class Pack
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Icon { get; set; }
    public int Version { get; set; } = 1;
    public int Order { get; set; }
    public List<PackSign> Signs { get; set; } = [];
    public List<PackPhrase> Phrases { get; set; } = [];
}

public class PackSign
{
    public required string PackId { get; set; }
    public required string SignId { get; set; }
    public Sign? Sign { get; set; }
    public int Order { get; set; }
}

public class PackPhrase
{
    public required string PackId { get; set; }
    public required string PhraseId { get; set; }
    public Phrase? Phrase { get; set; }
    public int Order { get; set; }
}

public class FavoriteSign
{
    public Guid UserId { get; set; }
    public required string SignId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class FavoritePhrase
{
    public Guid UserId { get; set; }
    public required string PhraseId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
