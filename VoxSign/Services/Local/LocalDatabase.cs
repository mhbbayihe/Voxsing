using SQLite;

namespace VoxSign.Services.Local;

/// <summary>
/// Base SQLite de l'appareil : cache du dictionnaire et des phrases, packs hors ligne, favoris,
/// et file des modifications à envoyer au serveur quand le réseau revient.
/// </summary>
public class LocalDatabase
{
    readonly Lazy<Task<SQLiteAsyncConnection>> _connection;

    public LocalDatabase() : this(Path.Combine(FileSystem.AppDataDirectory, "voxsign.db3"))
    {
    }

    public LocalDatabase(string path)
    {
        _connection = new Lazy<Task<SQLiteAsyncConnection>>(async () =>
        {
            var connection = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await connection.CreateTablesAsync(CreateFlags.None,
                typeof(CategoryRecord), typeof(SignRecord), typeof(ThemeRecord), typeof(PhraseRecord),
                typeof(PackRecord), typeof(FavoriteRecord), typeof(PendingChange));
            return connection;
        });
    }

    public Task<SQLiteAsyncConnection> GetAsync() => _connection.Value;
}

[Table("categories")]
public class CategoryRecord
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

[Table("signs")]
public class SignRecord
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    [Indexed] public string CategoryKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string UsageDescription { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    /// <summary>Mots-clés séparés par « | ».</summary>
    public string Keywords { get; set; } = string.Empty;
    /// <summary>Fait partie d'au moins un pack présent sur l'appareil.</summary>
    public bool IsOffline { get; set; }
}

[Table("themes")]
public class ThemeRecord
{
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PhraseCount { get; set; }
    public int SortOrder { get; set; }
}

[Table("phrases")]
public class PhraseRecord
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    [Indexed] public string ThemeKey { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string? Context { get; set; }
    public bool IsCustom { get; set; }
    public bool IsOffline { get; set; }
    public int SortOrder { get; set; }
}

[Table("packs")]
public class PackRecord
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SignCount { get; set; }
    public int PhraseCount { get; set; }
    public long SizeBytes { get; set; }
    /// <summary>Dernière version publiée connue.</summary>
    public int Version { get; set; }
    /// <summary>Version présente sur l'appareil (0 = non téléchargé).</summary>
    public int DownloadedVersion { get; set; }
    public string SignIds { get; set; } = string.Empty;
    public string PhraseIds { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

[Table("favorites")]
public class FavoriteRecord
{
    public const string SignKind = "sign";
    public const string PhraseKind = "phrase";

    /// <summary>« sign:merci », « phrase:cafe-emporter ».</summary>
    [PrimaryKey] public string Key { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static string KeyOf(string kind, string id) => $"{kind}:{id}";
}

/// <summary>Modification faite sur l'appareil, en attente d'envoi au serveur (rejouée dans l'ordre).</summary>
[Table("pending_changes")]
public class PendingChange
{
    public const string FavoriteSign = "favorite-sign";
    public const string FavoritePhrase = "favorite-phrase";
    public const string CustomPhrase = "custom-phrase";

    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    /// <summary><c>true</c> = ajout / mise à jour, <c>false</c> = suppression.</summary>
    public bool IsUpsert { get; set; }
    public DateTime CreatedAt { get; set; }
}
