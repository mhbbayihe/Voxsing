using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VoxSign.Api.Data;
using VoxSign.Api.Services;
using VoxSign.Shared;

namespace VoxSign.Api.Endpoints;

/// <summary>
/// Favoris et phrases personnelles. Les écritures sont idempotentes (PUT / DELETE) : l'application peut
/// rejouer sans risque les changements faits hors connexion.
/// </summary>
public static class FavoritesEndpoints
{
    const int MaxIdLength = 64;

    public static void MapFavoritesEndpoints(this IEndpointRouteBuilder app)
    {
        var favorites = app.MapGroup("/api/me/favorites").WithTags("Favoris").RequireAuthorization();
        favorites.MapGet("/", GetAsync);
        favorites.MapPut("/signs/{signId}", AddSignAsync);
        favorites.MapDelete("/signs/{signId}", RemoveSignAsync);
        favorites.MapPut("/phrases/{phraseId}", AddPhraseAsync);
        favorites.MapDelete("/phrases/{phraseId}", RemovePhraseAsync);

        var phrases = app.MapGroup("/api/me/phrases").WithTags("Phrases courantes").RequireAuthorization();
        phrases.MapPut("/{phraseId}", UpsertCustomPhraseAsync);
        phrases.MapDelete("/{phraseId}", DeleteCustomPhraseAsync);
    }

    static async Task<FavoritesDto> GetAsync(ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        // SQLite ne sait pas trier sur un DateTimeOffset : tri en mémoire (quelques dizaines de favoris au plus)
        var signs = await db.FavoriteSigns.Where(f => f.UserId == userId).ToListAsync(ct);
        var phrases = await db.FavoritePhrases.Where(f => f.UserId == userId).ToListAsync(ct);
        return new FavoritesDto(
            signs.OrderBy(f => f.CreatedAt).Select(f => f.SignId).ToList(),
            phrases.OrderBy(f => f.CreatedAt).Select(f => f.PhraseId).ToList());
    }

    static async Task<IResult> AddSignAsync(string signId, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        if (!await db.Signs.AnyAsync(s => s.Id == signId, ct))
            return Results.NotFound();
        if (!await db.FavoriteSigns.AnyAsync(f => f.UserId == userId && f.SignId == signId, ct))
        {
            db.FavoriteSigns.Add(new FavoriteSign { UserId = userId, SignId = signId });
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    static async Task<IResult> RemoveSignAsync(string signId, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        await db.FavoriteSigns.Where(f => f.UserId == userId && f.SignId == signId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    static async Task<IResult> AddPhraseAsync(string phraseId, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        if (!await db.Phrases.AnyAsync(p => p.Id == phraseId && (p.OwnerId == null || p.OwnerId == userId), ct))
            return Results.NotFound();
        if (!await db.FavoritePhrases.AnyAsync(f => f.UserId == userId && f.PhraseId == phraseId, ct))
        {
            db.FavoritePhrases.Add(new FavoritePhrase { UserId = userId, PhraseId = phraseId });
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    static async Task<IResult> RemovePhraseAsync(string phraseId, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        await db.FavoritePhrases.Where(f => f.UserId == userId && f.PhraseId == phraseId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    /// <summary>Crée ou modifie une phrase personnelle ; l'identifiant est choisi par l'appareil.</summary>
    static async Task<IResult> UpsertCustomPhraseAsync(string phraseId, CustomPhraseRequest request, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        var text = request.Text?.Trim() ?? string.Empty;
        var themeKey = string.IsNullOrWhiteSpace(request.ThemeKey) ? "accueil" : request.ThemeKey;
        var errors = new Dictionary<string, string[]>();
        if (phraseId.Length > MaxIdLength)
            errors["phraseId"] = [$"Identifiant trop long ({MaxIdLength} caractères au plus)."];
        if (text.Length is 0 or > TranslateEndpoints.MaxLength)
            errors["text"] = [$"La phrase doit compter entre 1 et {TranslateEndpoints.MaxLength} caractères."];
        if (!await db.Themes.AnyAsync(t => t.Key == themeKey, ct))
            errors["themeKey"] = ["Thème inconnu."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var phrase = await db.Phrases.Include(p => p.Theme).FirstOrDefaultAsync(p => p.Id == phraseId, ct);
        if (phrase is not null && phrase.OwnerId != userId)
            return Results.Problem(title: "Cet identifiant est déjà utilisé.", statusCode: StatusCodes.Status409Conflict);

        var created = phrase is null;
        phrase ??= db.Phrases.Add(new Phrase { Id = phraseId, Text = text, ThemeKey = themeKey, Intent = "Ma phrase", OwnerId = userId }).Entity;
        phrase.Text = text;
        phrase.ThemeKey = themeKey;
        phrase.Intent = string.IsNullOrWhiteSpace(request.Intent) ? "Ma phrase" : request.Intent.Trim();
        await db.SaveChangesAsync(ct);
        await db.Entry(phrase).Reference(p => p.Theme).LoadAsync(ct);

        return created ? Results.Created($"/api/phrases?search={Uri.EscapeDataString(text)}", phrase.ToDto()) : Results.Ok(phrase.ToDto());
    }

    static async Task<IResult> DeleteCustomPhraseAsync(string phraseId, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var userId = principal.UserId();
        await db.Phrases.Where(p => p.Id == phraseId && p.OwnerId == userId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }
}
