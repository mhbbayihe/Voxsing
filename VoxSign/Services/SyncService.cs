using System.Net;
using VoxSign.Services.Api;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.Services;

public interface ISyncService
{
    /// <summary>Après une synchronisation qui a modifié les données locales.</summary>
    event EventHandler? Synchronized;

    /// <summary>Lance une synchronisation en arrière-plan (sans attendre).</summary>
    void RequestSync();

    Task SyncAsync(CancellationToken ct = default);
}

/// <summary>
/// Envoie au serveur les modifications faites sur l'appareil (dans l'ordre, y compris celles faites hors ligne),
/// puis récupère les favoris et les phrases du compte. Se relance automatiquement au retour du réseau.
/// </summary>
public class SyncService : ISyncService
{
    readonly LocalDatabase _database;
    readonly IVoxSignApi _api;
    readonly ISessionStore _session;
    readonly IConnectivity _connectivity;
    readonly CatalogCache _catalog;
    readonly SemaphoreSlim _lock = new(1, 1);

    public SyncService(LocalDatabase database, IVoxSignApi api, ISessionStore session, IConnectivity connectivity, CatalogCache catalog)
    {
        _database = database;
        _api = api;
        _session = session;
        _connectivity = connectivity;
        _catalog = catalog;
        _connectivity.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
                RequestSync();
        };
    }

    public event EventHandler? Synchronized;

    public void RequestSync() => _ = Task.Run(async () =>
    {
        try
        {
            await SyncAsync();
        }
        catch (Exception)
        {
            // Nouvelle tentative au prochain retour du réseau ou à la prochaine modification
        }
    });

    public async Task SyncAsync(CancellationToken ct = default)
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet || !await _lock.WaitAsync(0, ct))
            return;

        try
        {
            var changed = false;
            if (_session.IsSignedIn && await PushPendingChangesAsync(ct))
            {
                await PullFavoritesAsync(ct);
                changed = true;
            }

            changed |= await _catalog.RefreshAsync(ct);
            if (changed)
                Synchronized?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <returns><c>true</c> si toute la file a été envoyée.</returns>
    async Task<bool> PushPendingChangesAsync(CancellationToken ct)
    {
        var db = await _database.GetAsync();
        var pending = await db.Table<PendingChange>().OrderBy(c => c.Id).ToListAsync();
        foreach (var change in pending)
        {
            try
            {
                switch (change.Kind)
                {
                    case PendingChange.FavoriteSign:
                        await _api.SetFavoriteAsync("sign", change.ItemId, change.IsUpsert, ct);
                        break;
                    case PendingChange.FavoritePhrase:
                        await _api.SetFavoriteAsync("phrase", change.ItemId, change.IsUpsert, ct);
                        break;
                    case PendingChange.CustomPhrase when change.IsUpsert:
                        if (await db.FindAsync<PhraseRecord>(change.ItemId) is { } phrase)
                            await _api.UpsertCustomPhraseAsync(phrase.Id, new CustomPhraseRequest(phrase.Text, phrase.ThemeKey, phrase.Intent), ct);
                        break;
                    case PendingChange.CustomPhrase:
                        await _api.DeleteCustomPhraseAsync(change.ItemId, ct);
                        break;
                }
            }
            catch (ApiException ex) when (ex.Status is HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                // Modification devenue sans objet (élément supprimé du catalogue) : on l'abandonne
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
            {
                return false;
            }

            await db.DeleteAsync<PendingChange>(change.Id);
        }

        return true;
    }

    /// <summary>Les favoris du serveur font foi une fois la file vidée.</summary>
    async Task PullFavoritesAsync(CancellationToken ct)
    {
        FavoritesDto favorites;
        try
        {
            favorites = await _api.GetFavoritesAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
        {
            return;
        }

        var db = await _database.GetAsync();
        var now = DateTime.UtcNow;
        await db.RunInTransactionAsync(tx =>
        {
            // Un favori modifié pendant la synchronisation : la prochaine passe l'enverra d'abord
            if (tx.Table<PendingChange>().Count() > 0)
                return;

            tx.DeleteAll<FavoriteRecord>();
            foreach (var (id, i) in favorites.SignIds.Select((id, i) => (id, i)))
                tx.Insert(new FavoriteRecord { Key = FavoriteRecord.KeyOf(FavoriteRecord.SignKind, id), Kind = FavoriteRecord.SignKind, ItemId = id, CreatedAt = now.AddTicks(i) });
            foreach (var (id, i) in favorites.PhraseIds.Select((id, i) => (id, i)))
                tx.Insert(new FavoriteRecord { Key = FavoriteRecord.KeyOf(FavoriteRecord.PhraseKind, id), Kind = FavoriteRecord.PhraseKind, ItemId = id, CreatedAt = now.AddTicks(i) });
        });
    }
}
