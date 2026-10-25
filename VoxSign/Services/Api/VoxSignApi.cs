using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VoxSign.Shared;

namespace VoxSign.Services.Api;

/// <summary>Erreur renvoyée par l'API (message lisible issu du « problem details »).</summary>
public class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Accès typé à l'API VoxSign. Les échecs réseau remontent en <see cref="HttpRequestException"/>.</summary>
public interface IVoxSignApi
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<PagedResult<SignDto>> GetSignsAsync(string? category, string? search, CancellationToken ct = default);
    Task<IReadOnlyList<ThemeDto>> GetThemesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PhraseDto>> GetPhrasesAsync(string? theme, CancellationToken ct = default);
    Task<TranslationDto> TranslateAsync(string text, CancellationToken ct = default);

    Task<FavoritesDto> GetFavoritesAsync(CancellationToken ct = default);
    Task SetFavoriteAsync(string kind, string id, bool isFavorite, CancellationToken ct = default);
    Task UpsertCustomPhraseAsync(string id, CustomPhraseRequest request, CancellationToken ct = default);
    Task DeleteCustomPhraseAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<PackSummaryDto>> GetPacksAsync(CancellationToken ct = default);
    Task<PackContentDto> GetPackAsync(string id, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string url, CancellationToken ct = default);
}

public class VoxSignApi(HttpClient http, ISessionStore session) : IVoxSignApi
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request, ct);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, ct);

    public Task<UserDto> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/me/preferences", request, ct);

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CategoryDto>>(HttpMethod.Get, "api/categories", null, ct);

    public Task<PagedResult<SignDto>> GetSignsAsync(string? category, string? search, CancellationToken ct = default) =>
        SendAsync<PagedResult<SignDto>>(HttpMethod.Get, $"api/signs?take=100&category={Uri.EscapeDataString(category ?? "")}&search={Uri.EscapeDataString(search ?? "")}", null, ct);

    public Task<IReadOnlyList<ThemeDto>> GetThemesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ThemeDto>>(HttpMethod.Get, "api/themes", null, ct);

    public Task<IReadOnlyList<PhraseDto>> GetPhrasesAsync(string? theme, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PhraseDto>>(HttpMethod.Get, $"api/phrases?theme={Uri.EscapeDataString(theme ?? "")}", null, ct);

    public Task<TranslationDto> TranslateAsync(string text, CancellationToken ct = default) =>
        SendAsync<TranslationDto>(HttpMethod.Post, "api/translate", new TranslateRequest(text), ct);

    public Task<FavoritesDto> GetFavoritesAsync(CancellationToken ct = default) =>
        SendAsync<FavoritesDto>(HttpMethod.Get, "api/me/favorites", null, ct);

    public Task SetFavoriteAsync(string kind, string id, bool isFavorite, CancellationToken ct = default) =>
        SendAsync<object>(isFavorite ? HttpMethod.Put : HttpMethod.Delete, $"api/me/favorites/{kind}s/{Uri.EscapeDataString(id)}", null, ct);

    public Task UpsertCustomPhraseAsync(string id, CustomPhraseRequest request, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/me/phrases/{Uri.EscapeDataString(id)}", request, ct);

    public Task DeleteCustomPhraseAsync(string id, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/me/phrases/{Uri.EscapeDataString(id)}", null, ct);

    public Task<IReadOnlyList<PackSummaryDto>> GetPacksAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PackSummaryDto>>(HttpMethod.Get, "api/packs", null, ct);

    public Task<PackContentDto> GetPackAsync(string id, CancellationToken ct = default) =>
        SendAsync<PackContentDto>(HttpMethod.Get, $"api/packs/{Uri.EscapeDataString(id)}", null, ct);

    public Task<byte[]> DownloadAsync(string url, CancellationToken ct = default) => http.GetByteArrayAsync(url, ct);

    async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (session.Token is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await TryReadProblemAsync(response, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized && session.Token is not null && !path.StartsWith("api/auth", StringComparison.Ordinal))
                session.Clear(); // jeton expiré ou révoqué
            throw new ApiException(response.StatusCode, problem ?? $"Erreur du serveur ({(int)response.StatusCode}).");
        }

        if (typeof(T) == typeof(object) || response.StatusCode == HttpStatusCode.NoContent)
            return default!;
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    static async Task<string?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(ct);
            var detail = problem?.Errors?.Values.SelectMany(v => v).FirstOrDefault();
            return detail ?? problem?.Title;
        }
        catch (Exception)
        {
            return null;
        }
    }

    sealed record ValidationProblem(string? Title, Dictionary<string, string[]>? Errors);
}
