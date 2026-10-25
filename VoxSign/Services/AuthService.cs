using VoxSign.Services.Api;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.Services;

public class AuthService(IVoxSignApi api, ISessionStore session, ISyncService sync, LocalDatabase database, IConnectivity connectivity) : IAuthService
{
    const string OfflineMessage = "Pas de connexion Internet. Vous pouvez explorer sans compte en attendant.";

    public UserDto? CurrentUser => session.User;

    public async Task<bool> RestoreSessionAsync()
    {
        await session.LoadAsync();
        if (session.IsSignedIn)
            sync.RequestSync();
        return session.IsSignedIn;
    }

    public Task<AuthResult> SignInAsync(string email, string password) =>
        AuthenticateAsync(() => api.LoginAsync(new LoginRequest(email.Trim(), password)));

    public Task<AuthResult> RegisterAsync(string firstName, string email, string password, string signLanguage) =>
        AuthenticateAsync(() => api.RegisterAsync(new RegisterRequest(firstName.Trim(), email.Trim(), password, signLanguage)));

    async Task<AuthResult> AuthenticateAsync(Func<Task<AuthResponse>> call)
    {
        if (connectivity.NetworkAccess != NetworkAccess.Internet)
            return new AuthResult(false, OfflineMessage);

        try
        {
            await session.SaveAsync(await call());
            // Les favoris marqués avant connexion rejoignent le compte, puis on récupère ceux du compte
            await sync.SyncAsync();
            return AuthResult.Success;
        }
        catch (ApiException ex)
        {
            return new AuthResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AuthResult(false, "Le serveur VoxSign ne répond pas. Réessayez dans un instant.");
        }
    }

    public async Task<AuthResult> UpdateSpeechPreferencesAsync(SpeechPreferencesDto preferences)
    {
        if (!session.IsSignedIn)
            return new AuthResult(false, "Connectez-vous pour enregistrer vos préférences.");

        try
        {
            session.UpdateUser(await api.UpdatePreferencesAsync(new UpdatePreferencesRequest(null, preferences)));
            return AuthResult.Success;
        }
        catch (ApiException ex)
        {
            return new AuthResult(false, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AuthResult(false, OfflineMessage);
        }
    }

    public void ContinueAsGuest() => session.Clear();

    public async Task SignOutAsync()
    {
        session.Clear();
        var db = await database.GetAsync();
        await db.RunInTransactionAsync(tx =>
        {
            tx.DeleteAll<FavoriteRecord>();
            tx.DeleteAll<PendingChange>();
            tx.Execute("DELETE FROM phrases WHERE IsCustom = 1");
        });
    }
}
