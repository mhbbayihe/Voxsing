using System.Text.Json;
using VoxSign.Shared;

namespace VoxSign.Services.Api;

/// <summary>Session de l'utilisateur connecté, conservée entre deux lancements (jeton en stockage sécurisé).</summary>
public interface ISessionStore
{
    string? Token { get; }

    UserDto? User { get; }

    bool IsSignedIn { get; }

    Task LoadAsync();

    Task SaveAsync(AuthResponse auth);

    void UpdateUser(UserDto user);

    void Clear();
}

public class SessionStore(ISecureStorage secureStorage, IPreferences preferences) : ISessionStore
{
    const string TokenKey = "session_token";
    const string UserKey = "session_user";
    const string ExpiresKey = "session_expires";

    public string? Token { get; private set; }

    public UserDto? User { get; private set; }

    public bool IsSignedIn => Token is not null && User is not null;

    public async Task LoadAsync()
    {
        try
        {
            var expires = preferences.Get(ExpiresKey, DateTime.MinValue);
            var userJson = preferences.Get<string?>(UserKey, null);
            if (expires <= DateTime.UtcNow || userJson is null)
            {
                Clear();
                return;
            }

            Token = await secureStorage.GetAsync(TokenKey);
            User = JsonSerializer.Deserialize<UserDto>(userJson, JsonSerializerOptions.Web);
        }
        catch (Exception)
        {
            // Stockage sécurisé indisponible ou corrompu : on repart d'une session vide
            Clear();
        }
    }

    public async Task SaveAsync(AuthResponse auth)
    {
        Token = auth.Token;
        User = auth.User;
        await secureStorage.SetAsync(TokenKey, auth.Token);
        preferences.Set(ExpiresKey, auth.ExpiresAt.UtcDateTime);
        preferences.Set(UserKey, JsonSerializer.Serialize(auth.User, JsonSerializerOptions.Web));
    }

    public void UpdateUser(UserDto user)
    {
        User = user;
        preferences.Set(UserKey, JsonSerializer.Serialize(user, JsonSerializerOptions.Web));
    }

    public void Clear()
    {
        Token = null;
        User = null;
        secureStorage.Remove(TokenKey);
        preferences.Remove(UserKey);
        preferences.Remove(ExpiresKey);
    }
}
