using VoxSign.Models;
using VoxSign.Services.Api;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.Services;

public class ContentService(LocalDatabase database, CatalogCache catalog, IVoxSignApi api, IFavoritesService favorites, ISyncService sync) : IContentService
{
    static readonly TimeSpan ServerBudget = TimeSpan.FromSeconds(1.5);

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        var db = await OpenAsync();
        var categories = await db.Table<CategoryRecord>().OrderBy(c => c.SortOrder).ToListAsync();
        var signs = await db.Table<SignRecord>().ToListAsync();
        return categories.Select(c => new CategoryDto(c.Key, c.Label, c.ShortLabel, signs.Count(s => s.CategoryKey == c.Key))).ToList();
    }

    public async Task<IReadOnlyList<Sign>> GetSignsAsync(string? categoryKey = null, string? search = null)
    {
        var db = await OpenAsync();
        var records = await db.Table<SignRecord>().ToListAsync();
        var filtered = records
            .Where(s => string.IsNullOrEmpty(categoryKey) || s.CategoryKey == categoryKey)
            .Where(s => string.IsNullOrWhiteSpace(search) || TextSearch.Contains(s.Word, search) || s.Keywords.Split('|').Any(k => TextSearch.Contains(k, search)))
            .ToList();
        return await ToSignsAsync(filtered);
    }

    public async Task<Sign?> GetSignAsync(string id)
    {
        var db = await OpenAsync();
        var record = await db.FindAsync<SignRecord>(id);
        return record is null ? null : (await ToSignsAsync([record]))[0];
    }

    public async Task<int> GetCatalogSizeAsync() => await (await OpenAsync()).Table<SignRecord>().CountAsync();

    public async Task<int> GetOfflineSignCountAsync() => await (await OpenAsync()).Table<SignRecord>().Where(s => s.IsOffline).CountAsync();

    public async Task<IReadOnlyList<ThemeDto>> GetThemesAsync()
    {
        var db = await OpenAsync();
        var themes = await db.Table<ThemeRecord>().OrderBy(t => t.SortOrder).ToListAsync();
        return themes.Select(t => new ThemeDto(t.Key, t.Label, t.Title, t.Description, t.PhraseCount)).ToList();
    }

    public async Task<IReadOnlyList<Phrase>> GetPhrasesAsync(string? themeKey = null, string? search = null)
    {
        var db = await OpenAsync();
        var records = await db.Table<PhraseRecord>().OrderBy(p => p.SortOrder).ToListAsync();
        var filtered = records
            .Where(p => string.IsNullOrEmpty(themeKey) || p.ThemeKey == themeKey)
            .Where(p => string.IsNullOrWhiteSpace(search) || TextSearch.Contains(p.Text, search))
            .OrderBy(p => p.IsCustom)
            .ToList();
        return await ToPhrasesAsync(filtered);
    }

    public async Task<IReadOnlyList<Phrase>> GetFavoritePhrasesAsync()
    {
        var ids = await favorites.GetIdsAsync(FavoriteRecord.PhraseKind);
        var db = await OpenAsync();
        var records = new List<PhraseRecord>();
        foreach (var id in ids)
            if (await db.FindAsync<PhraseRecord>(id) is { } record)
                records.Add(record);
        return await ToPhrasesAsync(records);
    }

    public async Task<IReadOnlyList<Sign>> GetFavoriteSignsAsync()
    {
        var ids = await favorites.GetIdsAsync(FavoriteRecord.SignKind);
        var db = await OpenAsync();
        var records = new List<SignRecord>();
        foreach (var id in ids)
            if (await db.FindAsync<SignRecord>(id) is { } record)
                records.Add(record);
        return await ToSignsAsync(records);
    }

    public async Task<IReadOnlyList<Sign>> TranslateAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var db = await OpenAsync();
        if (catalog.IsOnline)
        {
            try
            {
                // Délai court : si le serveur tarde, la traduction sur l'appareil donne le même résultat
                using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
                budget.CancelAfter(ServerBudget);
                var translation = await api.TranslateAsync(text.Trim(), budget.Token);
                // Les repères reçus rejoignent la copie locale : ils resteront disponibles hors ligne
                foreach (var sign in translation.Signs.DistinctBy(s => s.Id))
                {
                    var existing = await db.FindAsync<SignRecord>(sign.Id);
                    await db.InsertOrReplaceAsync(CatalogCache.ToRecord(sign, existing?.IsOffline ?? false));
                }

                var records = new List<SignRecord>();
                foreach (var sign in translation.Signs)
                    records.Add((await db.FindAsync<SignRecord>(sign.Id))!);
                return await ToSignsAsync(records);
            }
            catch (Exception ex) when (ex is HttpRequestException or ApiException || ex is TaskCanceledException && !ct.IsCancellationRequested)
            {
                // Serveur injoignable : traduction sur l'appareil
            }
        }

        return await TranslateOnDeviceAsync(text);
    }

    public async Task<IReadOnlyList<Sign>> TranslateOnDeviceAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var db = await OpenAsync();
        var cached = await db.Table<SignRecord>().ToListAsync();
        var byId = cached.ToDictionary(s => s.Id);
        var sequencer = new SignSequencer(cached.Select(s => (s.Id, s.Word, (IEnumerable<string>)s.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries))));
        return await ToSignsAsync(sequencer.Sequence(text).Select(id => byId[id]).ToList());
    }

    public async Task<Phrase> SaveCustomPhraseAsync(string text, string themeKey = "accueil")
    {
        var db = await OpenAsync();
        var theme = await db.FindAsync<ThemeRecord>(themeKey);
        var record = new PhraseRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = text.Trim(),
            ThemeKey = themeKey,
            Theme = theme?.Label ?? themeKey,
            Intent = "Ma phrase",
            Context = "Ma phrase",
            IsCustom = true,
            IsOffline = true,
            SortOrder = int.MaxValue,
        };
        await db.RunInTransactionAsync(tx =>
        {
            tx.Insert(record);
            tx.Insert(new PendingChange { Kind = PendingChange.CustomPhrase, ItemId = record.Id, IsUpsert = true, CreatedAt = DateTime.UtcNow });
        });
        sync.RequestSync();
        return (await ToPhrasesAsync([record]))[0];
    }

    async Task<SQLite.SQLiteAsyncConnection> OpenAsync()
    {
        await catalog.EnsureInitializedAsync();
        return await database.GetAsync();
    }

    async Task<IReadOnlyList<Sign>> ToSignsAsync(IReadOnlyList<SignRecord> records)
    {
        var favoriteIds = (await favorites.GetIdsAsync(FavoriteRecord.SignKind)).ToHashSet();
        return records.Select(r => new Sign
        {
            Id = r.Id,
            Word = r.Word,
            Category = r.Category,
            CategoryKey = r.CategoryKey,
            Image = MediaStore.Resolve(r.ImageKey, r.ImageUrl),
            Usage = r.Usage,
            UsageDescription = r.UsageDescription,
            Example = r.Example,
            IsOffline = r.IsOffline,
            IsFavorite = favoriteIds.Contains(r.Id),
        }).ToList();
    }

    async Task<IReadOnlyList<Phrase>> ToPhrasesAsync(IReadOnlyList<PhraseRecord> records)
    {
        var favoriteIds = (await favorites.GetIdsAsync(FavoriteRecord.PhraseKind)).ToHashSet();
        return records.Select(r => new Phrase
        {
            Id = r.Id,
            Text = r.Text,
            ThemeKey = r.ThemeKey,
            Theme = r.Theme,
            Intent = r.Intent,
            Context = r.Context,
            IsCustom = r.IsCustom,
            IsFavorite = favoriteIds.Contains(r.Id),
        }).ToList();
    }
}
