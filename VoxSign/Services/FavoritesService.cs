using VoxSign.Services.Local;

namespace VoxSign.Services;

public interface IFavoritesService
{
    /// <summary>Identifiants favoris d'un type (<see cref="FavoriteRecord.SignKind"/> ou <see cref="FavoriteRecord.PhraseKind"/>), du plus ancien au plus récent.</summary>
    Task<IReadOnlyList<string>> GetIdsAsync(string kind);

    /// <summary>Enregistre le changement sur l'appareil immédiatement, puis l'envoie au serveur dès que possible.</summary>
    Task SetAsync(string kind, string id, bool isFavorite);
}

public class FavoritesService(LocalDatabase database, ISyncService sync) : IFavoritesService
{
    public async Task<IReadOnlyList<string>> GetIdsAsync(string kind)
    {
        var db = await database.GetAsync();
        var records = await db.Table<FavoriteRecord>().Where(f => f.Kind == kind).OrderBy(f => f.CreatedAt).ToListAsync();
        return records.Select(f => f.ItemId).ToList();
    }

    public async Task SetAsync(string kind, string id, bool isFavorite)
    {
        var db = await database.GetAsync();
        var changeKind = kind == FavoriteRecord.SignKind ? PendingChange.FavoriteSign : PendingChange.FavoritePhrase;
        await db.RunInTransactionAsync(tx =>
        {
            var key = FavoriteRecord.KeyOf(kind, id);
            if (isFavorite)
                tx.InsertOrReplace(new FavoriteRecord { Key = key, Kind = kind, ItemId = id, CreatedAt = DateTime.UtcNow });
            else
                tx.Delete<FavoriteRecord>(key);

            // Seul le dernier état compte : on remplace un changement encore en attente pour le même élément
            tx.Execute("DELETE FROM pending_changes WHERE Kind = ? AND ItemId = ?", changeKind, id);
            tx.Insert(new PendingChange { Kind = changeKind, ItemId = id, IsUpsert = isFavorite, CreatedAt = DateTime.UtcNow });
        });
        sync.RequestSync();
    }
}
