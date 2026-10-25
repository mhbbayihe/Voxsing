using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VoxSign.Api.Data;
using VoxSign.Api.Services;
using VoxSign.Shared;

namespace VoxSign.Api.Endpoints;

/// <summary>Dictionnaire de signes illustrés et phrases courantes.</summary>
public static class CatalogEndpoints
{
    const int MaxPageSize = 100;

    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Dictionnaire et phrases");
        api.MapGet("/categories", GetCategoriesAsync);
        api.MapGet("/signs", GetSignsAsync);
        api.MapGet("/signs/{id}", GetSignAsync);
        api.MapGet("/themes", GetThemesAsync);
        api.MapGet("/phrases", GetPhrasesAsync);
    }

    static async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(VoxSignDbContext db, CancellationToken ct) =>
        await db.Categories
            .OrderBy(c => c.Order)
            .Select(c => new CategoryDto(c.Key, c.Label, c.ShortLabel, db.Signs.Count(s => s.CategoryKey == c.Key)))
            .ToListAsync(ct);

    static async Task<PagedResult<SignDto>> GetSignsAsync(
        HttpRequest request, VoxSignDbContext db, CancellationToken ct,
        string? category = null, string? search = null, int skip = 0, int take = 50)
    {
        var query = db.Signs.Include(s => s.Category).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.CategoryKey == category);

        // Recherche sans accents ni casse, en mémoire (le dictionnaire reste de taille modeste)
        var signs = await query.OrderBy(s => s.Word).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
        {
            signs = signs.Where(s => TextSearch.Contains(s.Word, search) || s.Keywords.Any(k => TextSearch.Contains(k, search))).ToList();
        }

        var baseUrl = request.ImageBaseUrl();
        var page = signs.Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, MaxPageSize)).Select(s => s.ToDto(baseUrl)).ToList();
        return new PagedResult<SignDto>(page, signs.Count);
    }

    static async Task<IResult> GetSignAsync(string id, HttpRequest request, VoxSignDbContext db, CancellationToken ct)
    {
        var sign = await db.Signs.Include(s => s.Category).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return sign is null ? Results.NotFound() : Results.Ok(sign.ToDto(request.ImageBaseUrl()));
    }

    static async Task<IReadOnlyList<ThemeDto>> GetThemesAsync(VoxSignDbContext db, CancellationToken ct) =>
        await db.Themes
            .OrderBy(t => t.Order)
            .Select(t => new ThemeDto(t.Key, t.Label, t.Title, t.Description, db.Phrases.Count(p => p.ThemeKey == t.Key && p.OwnerId == null)))
            .ToListAsync(ct);

    /// <summary>Phrases du catalogue, plus les phrases personnelles de l'utilisateur connecté.</summary>
    static async Task<IReadOnlyList<PhraseDto>> GetPhrasesAsync(ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct, string? theme = null, string? search = null)
    {
        var userId = principal.UserIdOrNull();
        var query = db.Phrases.Include(p => p.Theme).AsNoTracking().Where(p => p.OwnerId == null || p.OwnerId == userId);
        if (!string.IsNullOrWhiteSpace(theme))
            query = query.Where(p => p.ThemeKey == theme);

        var phrases = await query.OrderBy(p => p.OwnerId == null ? 0 : 1).ThenBy(p => p.Order).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
            phrases = phrases.Where(p => TextSearch.Contains(p.Text, search)).ToList();

        return phrases.Select(p => p.ToDto()).ToList();
    }
}
