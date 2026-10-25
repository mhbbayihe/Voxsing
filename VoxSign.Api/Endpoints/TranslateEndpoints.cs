using Microsoft.EntityFrameworkCore;
using VoxSign.Api.Data;
using VoxSign.Api.Services;
using VoxSign.Shared;

namespace VoxSign.Api.Endpoints;

/// <summary>Saisie texte : découpage d'une phrase en repères illustrés.</summary>
public static class TranslateEndpoints
{
    public const int MaxLength = 300;

    public static void MapTranslateEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/translate", TranslateAsync).WithTags("Saisie texte");

    static async Task<IResult> TranslateAsync(TranslateRequest request, HttpRequest http, VoxSignDbContext db, CancellationToken ct)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > MaxLength)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["text"] = [$"Le texte doit compter entre 1 et {MaxLength} caractères."] });

        var signs = await db.Signs.Include(s => s.Category).AsNoTracking().ToListAsync(ct);
        var sequencer = new SignSequencer(signs.Select(s => (s.Id, s.Word, (IEnumerable<string>)s.Keywords)));
        var tokens = sequencer.Tokenize(text);

        var baseUrl = http.ImageBaseUrl();
        var byId = signs.ToDictionary(s => s.Id);
        var sequence = tokens.Where(t => t.SignId is not null).Select(t => byId[t.SignId!].ToDto(baseUrl)).ToList();
        return Results.Ok(new TranslationDto(text, tokens.Select(t => new TranslationTokenDto(t.Text, t.SignId)).ToList(), sequence));
    }
}
