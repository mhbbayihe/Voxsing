using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using VoxSign.Api.Data;
using VoxSign.Api.Services;
using VoxSign.Shared;

namespace VoxSign.Api.Endpoints;

/// <summary>Mode hors ligne : packs de repères et de phrases à télécharger sur l'appareil.</summary>
public static class PackEndpoints
{
    public static void MapPackEndpoints(this IEndpointRouteBuilder app)
    {
        var packs = app.MapGroup("/api/packs").WithTags("Hors ligne");
        packs.MapGet("/", GetPacksAsync);
        packs.MapGet("/{id}", GetPackContentAsync);
    }

    static IQueryable<Pack> WithContent(VoxSignDbContext db) => db.Packs
        .AsNoTracking()
        .Include(p => p.Signs).ThenInclude(s => s.Sign).ThenInclude(s => s!.Category)
        .Include(p => p.Phrases).ThenInclude(p => p.Phrase).ThenInclude(p => p!.Theme)
        .AsSplitQuery();

    static PackSummaryDto Summarize(Pack pack, PackSizer sizer) => new(
        pack.Id,
        pack.Name,
        pack.Icon,
        pack.Signs.Count,
        pack.Phrases.Count,
        sizer.Estimate(pack.Signs.Select(s => s.Sign!.ImageKey), pack.Signs.Count + pack.Phrases.Count),
        pack.Version);

    static async Task<IReadOnlyList<PackSummaryDto>> GetPacksAsync(VoxSignDbContext db, PackSizer sizer, CancellationToken ct)
    {
        var packs = await WithContent(db).OrderBy(p => p.Order).ToListAsync(ct);
        return packs.Select(p => Summarize(p, sizer)).ToList();
    }

    /// <summary>Contenu complet ; l'en-tête ETag (version du pack) permet à l'appareil de savoir s'il est à jour.</summary>
    static async Task<IResult> GetPackContentAsync(string id, HttpContext http, VoxSignDbContext db, PackSizer sizer, CancellationToken ct)
    {
        var pack = await WithContent(db).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pack is null)
            return Results.NotFound();

        var etag = new EntityTagHeaderValue($"\"{pack.Id}-v{pack.Version}\"");
        if (http.Request.Headers.IfNoneMatch.ToString() == etag.Tag.ToString())
            return Results.StatusCode(StatusCodes.Status304NotModified);

        http.Response.Headers.ETag = etag.ToString();
        var baseUrl = http.Request.ImageBaseUrl();
        return Results.Ok(new PackContentDto(
            Summarize(pack, sizer),
            pack.Signs.OrderBy(s => s.Order).Select(s => s.Sign!.ToDto(baseUrl)).ToList(),
            pack.Phrases.OrderBy(p => p.Order).Select(p => p.Phrase!.ToDto()).ToList()));
    }
}
