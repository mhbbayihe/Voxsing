using VoxSign.Models;
using VoxSign.Services.Api;
using VoxSign.Services.Local;

namespace VoxSign.Services;

public interface IOfflinePackService
{
    /// <summary>Packs connus de l'appareil ; avec <paramref name="refresh"/>, liste et versions sont d'abord mises à jour depuis le serveur.</summary>
    Task<IReadOnlyList<OfflinePack>> GetPacksAsync(bool refresh = false);

    /// <summary>Télécharge (ou met à jour) le contenu et les images d'un pack sur l'appareil.</summary>
    Task DownloadAsync(OfflinePack pack, CancellationToken ct = default);

    /// <summary>Retire un pack de l'appareil (les images embarquées dans l'application restent disponibles).</summary>
    Task RemoveAsync(OfflinePack pack);
}

public class OfflinePackService(LocalDatabase database, CatalogCache catalog, IVoxSignApi api) : IOfflinePackService
{
    public async Task<IReadOnlyList<OfflinePack>> GetPacksAsync(bool refresh = false)
    {
        await catalog.EnsureInitializedAsync();
        var db = await database.GetAsync();

        if (refresh && catalog.IsOnline)
        {
            try
            {
                foreach (var (summary, i) in (await api.GetPacksAsync()).Select((p, i) => (p, i)))
                {
                    var record = await db.FindAsync<PackRecord>(summary.Id) ?? new PackRecord { Id = summary.Id };
                    record.Name = summary.Name;
                    record.Icon = summary.Icon;
                    record.SignCount = summary.SignCount;
                    record.PhraseCount = summary.PhraseCount;
                    record.SizeBytes = summary.SizeBytes;
                    record.Version = summary.Version;
                    record.SortOrder = i;
                    await db.InsertOrReplaceAsync(record);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
            {
                // Hors connexion de fait : on affiche les packs connus
            }
        }

        var packs = await db.Table<PackRecord>().OrderBy(p => p.SortOrder).ToListAsync();
        return packs.Select(p => new OfflinePack
        {
            Id = p.Id,
            Name = p.Name,
            Icon = p.Icon,
            SignCount = p.SignCount,
            PhraseCount = p.PhraseCount,
            SizeBytes = p.SizeBytes,
            IsDownloaded = p.DownloadedVersion > 0,
            HasUpdate = p.DownloadedVersion > 0 && p.DownloadedVersion < p.Version,
        }).ToList();
    }

    public async Task DownloadAsync(OfflinePack pack, CancellationToken ct = default)
    {
        var content = await api.GetPackAsync(pack.Id, ct);

        // Images d'abord : le pack n'est marqué téléchargé que si tout est présent
        Directory.CreateDirectory(MediaStore.Directory);
        foreach (var sign in content.Signs.Where(s => !MediaStore.IsBundled(s.ImageKey) && !File.Exists(MediaStore.LocalPath(s.ImageKey))))
        {
            var bytes = await api.DownloadAsync(sign.ImageUrl, ct);
            var temporary = MediaStore.LocalPath(sign.ImageKey) + ".part";
            await File.WriteAllBytesAsync(temporary, bytes, ct);
            File.Move(temporary, MediaStore.LocalPath(sign.ImageKey), overwrite: true);
        }

        var db = await database.GetAsync();
        await db.RunInTransactionAsync(tx =>
        {
            foreach (var sign in content.Signs)
                tx.InsertOrReplace(CatalogCache.ToRecord(sign, isOffline: true));
            var order = tx.ExecuteScalar<int>("SELECT COALESCE(MAX(SortOrder), 0) FROM phrases WHERE IsCustom = 0");
            foreach (var phrase in content.Phrases)
            {
                var existing = tx.Find<PhraseRecord>(phrase.Id);
                tx.InsertOrReplace(CatalogCache.ToRecord(phrase, isOffline: true, existing?.SortOrder ?? ++order));
            }

            var record = tx.Find<PackRecord>(pack.Id) ?? new PackRecord { Id = pack.Id };
            record.Name = content.Pack.Name;
            record.Icon = content.Pack.Icon;
            record.SignCount = content.Pack.SignCount;
            record.PhraseCount = content.Pack.PhraseCount;
            record.SizeBytes = content.Pack.SizeBytes;
            record.Version = content.Pack.Version;
            record.DownloadedVersion = content.Pack.Version;
            record.SignIds = string.Join('|', content.Signs.Select(s => s.Id));
            record.PhraseIds = string.Join('|', content.Phrases.Select(p => p.Id));
            tx.InsertOrReplace(record);
        });

        pack.IsDownloaded = true;
        pack.HasUpdate = false;
    }

    public async Task RemoveAsync(OfflinePack pack)
    {
        var db = await database.GetAsync();
        var removedImages = new List<string>();
        await db.RunInTransactionAsync(tx =>
        {
            if (tx.Find<PackRecord>(pack.Id) is not { } record)
                return;

            record.DownloadedVersion = 0;
            tx.Update(record);

            // Un repère ou une phrase reste hors ligne s'il appartient encore à un autre pack téléchargé
            var kept = tx.Table<PackRecord>().Where(p => p.DownloadedVersion > 0).ToList();
            var keptSigns = kept.SelectMany(p => p.SignIds.Split('|')).ToHashSet();
            var keptPhrases = kept.SelectMany(p => p.PhraseIds.Split('|')).ToHashSet();
            foreach (var id in record.SignIds.Split('|', StringSplitOptions.RemoveEmptyEntries).Where(id => !keptSigns.Contains(id)))
            {
                if (tx.Find<SignRecord>(id) is not { } sign)
                    continue;
                sign.IsOffline = false;
                tx.Update(sign);
                if (!MediaStore.IsBundled(sign.ImageKey))
                    removedImages.Add(sign.ImageKey);
            }

            foreach (var id in record.PhraseIds.Split('|', StringSplitOptions.RemoveEmptyEntries).Where(id => !keptPhrases.Contains(id)))
                tx.Execute("UPDATE phrases SET IsOffline = 0 WHERE Id = ? AND IsCustom = 0", id);
        });

        foreach (var key in removedImages)
            File.Delete(MediaStore.LocalPath(key));

        pack.IsDownloaded = false;
        pack.HasUpdate = false;
    }
}
