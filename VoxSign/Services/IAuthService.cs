using VoxSign.Shared;

namespace VoxSign.Services;

public record AuthResult(bool Succeeded, string? Error = null)
{
    public static AuthResult Success { get; } = new(true);
}

public interface IAuthService
{
    UserDto? CurrentUser { get; }

    /// <summary>Restaure la session enregistrée (l'application reste utilisable hors ligne une fois connecté).</summary>
    Task<bool> RestoreSessionAsync();

    Task<AuthResult> SignInAsync(string email, string password);

    Task<AuthResult> RegisterAsync(string firstName, string email, string password, string signLanguage);

    Task<AuthResult> UpdateSpeechPreferencesAsync(SpeechPreferencesDto preferences);

    void ContinueAsGuest();

    /// <summary>Déconnexion : efface la session et les données personnelles de l'appareil.</summary>
    Task SignOutAsync();
}
