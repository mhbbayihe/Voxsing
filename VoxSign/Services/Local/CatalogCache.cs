using VoxSign.Services.Api;
using VoxSign.Shared;

namespace VoxSign.Services.Local;

/// <summary>
/// Copie locale du dictionnaire et des phrases. Remplie au premier lancement avec le contenu embarqué
/// (packs « Les essentiels » et « Au quotidien »), puis rafraîchie depuis le serveur dès que le réseau le permet.
/// </summary>
public class CatalogCache(LocalDatabase database, IVoxSignApi api, IConnectivity connectivity)
{
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    Task? _initialization;

    public bool IsOnline => connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task EnsureInitializedAsync() => _initialization ??= InitializeAsync();

    async Task InitializeAsync()
    {
        var db = await database.GetAsync();
        if (await db.Table<CategoryRecord>().CountAsync() > 0)
            return;

        var bundledPacks = SeedCatalog.Packs.Where(p => SeedCatalog.BundledPackIds.Contains(p.Id)).ToList();
        var offlineSigns = bundledPacks.SelectMany(p => p.SignIds).ToHashSet();
        var offlinePhrases = bundledPacks.SelectMany(p => p.PhraseIds).ToHashSet();
        var imageBase = new Uri(new Uri(ApiSettings.BaseUrl), "media/signs").ToString();

        await db.RunInTransactionAsync(tx =>
        {
            foreach (var (category, i) in SeedCatalog.Categories.Select((c, i) => (c, i)))
                tx.InsertOrReplace(new CategoryRecord { Key = category.Key, Label = category.Label, ShortLabel = category.ShortLabel, SortOrder = i });
            foreach (var sign in SeedCatalog.Signs)
                tx.InsertOrReplace(ToRecord(SeedCatalog.ToDto(sign, imageBase), offlineSigns.Contains(sign.Id)));
            foreach (var (theme, i) in SeedCatalog.Themes.Select((t, i) => (t, i)))
                tx.InsertOrReplace(new ThemeRecord
                {
                    Key = theme.Key, Label = theme.Label, Title = theme.Title, Description = theme.Description, SortOrder = i,
                    PhraseCount = SeedCatalog.Phrases.Count(p => p.ThemeKey == theme.Key),
                });
            foreach (var (phrase, i) in SeedCatalog.Phrases.Select((p, i) => (p, i)))
                tx.InsertOrReplace(ToRecord(SeedCatalog.ToDto(phrase), offlinePhrases.Contains(phrase.Id), i));
            foreach (var (pack, i) in SeedCatalog.Packs.Select((p, i) => (p, i)))
                tx.InsertOrReplace(new PackRecord
                {
                    Id = pack.Id, Name = pack.Name, Icon = pack.Icon, SortOrder = i,
                    SignCount = pack.SignIds.Length, PhraseCount = pack.PhraseIds.Length, Version = pack.Version,
                    DownloadedVersion = SeedCatalog.BundledPackIds.Contains(pack.Id) ? pack.Version : 0,
                    SignIds = string.Join('|', pack.SignIds), PhraseIds = string.Join('|', pack.PhraseIds),
                });
        });
    }

    /// <summary>Met à jour la copie locale depuis le serveur ; sans effet hors connexion ou si le serveur ne répond pas.</summary>
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync();
        if (!IsOnline || !await _refreshLock.WaitAsync(0, ct))
            return false;

        try
        {
            var categories = await api.GetCategoriesAsync(ct);
            var signs = (await api.GetSignsAsync(null, null, ct)).Items;
            var themes = await api.GetThemesAsync(ct);
            var phrases = await api.GetPhrasesAsync(null, ct);

            var db = await database.GetAsync();
            var offlineSigns = (await db.Table<SignRecord>().Where(s => s.IsOffline).ToListAsync()).Select(s => s.Id).ToHashSet();
            var localPhrases = await db.Table<PhraseRecord>().ToListAsync();
            var pendingPhrases = (await db.Table<PendingChange>().Where(c => c.Kind == PendingChange.CustomPhrase).ToListAsync()).Select(c => c.ItemId).ToHashSet();

            await db.RunInTransactionAsync(tx =>
            {
                foreach (var (category, i) in categories.Select((c, i) => (c, i)))
                    tx.InsertOrReplace(new CategoryRecord { Key = category.Key, Label = category.Label, ShortLabel = category.ShortLabel, SortOrder = i });
                foreach (var (theme, i) in themes.Select((t, i) => (t, i)))
                    tx.InsertOrReplace(new ThemeRecord { Key = theme.Key, Label = theme.Label, Title = theme.Title, Description = theme.Description, PhraseCount = theme.PhraseCount, SortOrder = i });

                foreach (var sign in signs)
                    tx.InsertOrReplace(ToRecord(sign, offlineSigns.Contains(sign.Id)));
                // Repères retirés du serveur : conservés s'ils font partie d'un pack téléchargé
                var serverSigns = signs.Select(s => s.Id).ToHashSet();
                tx.Execute($"DELETE FROM signs WHERE IsOffline = 0 AND Id NOT IN ({Placeholders(serverSigns.Count)})", serverSigns.Cast<object>().ToArray());

                var offlinePhrases = localPhrases.Where(p => p.IsOffline).Select(p => p.Id).ToHashSet();
                foreach (var (phrase, i) in phrases.Select((p, i) => (p, i)))
                    tx.InsertOrReplace(ToRecord(phrase, offlinePhrases.Contains(phrase.Id) || phrase.IsCustom, i));
                var serverPhrases = phrases.Select(p => p.Id).ToHashSet();
                foreach (var stale in localPhrases.Where(p => !serverPhrases.Contains(p.Id) && !pendingPhrases.Contains(p.Id) && (p.IsCustom || !p.IsOffline)))
                    tx.Delete<PhraseRecord>(stale.Id);
            });
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    static string Placeholders(int count) => count == 0 ? "''" : string.Join(',', Enumerable.Repeat("?", count));

    public static SignRecord ToRecord(SignDto sign, bool isOffline) => new()
    {
        Id = sign.Id,
        Word = sign.Word,
        CategoryKey = sign.CategoryKey,
        Category = sign.Category,
        ImageKey = sign.ImageKey,
        ImageUrl = sign.ImageUrl,
        Usage = sign.Usage,
        UsageDescription = sign.UsageDescription,
        Example = sign.Example,
        Keywords = string.Join('|', sign.Keywords),
        IsOffline = isOffline,
    };

    public static PhraseRecord ToRecord(PhraseDto phrase, bool isOffline, int order) => new()
    {
        Id = phrase.Id,
        Text = phrase.Text,
        ThemeKey = phrase.ThemeKey,
        Theme = phrase.Theme,
        Intent = phrase.Intent,
        Context = phrase.Context,
        IsCustom = phrase.IsCustom,
        IsOffline = isOffline,
        SortOrder = order,
    };
}
