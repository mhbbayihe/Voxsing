using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoxSign.Api.Data;
using VoxSign.Api.Services;
using VoxSign.Shared;

namespace VoxSign.Api.Endpoints;

public static class AuthEndpoints
{
    const int MinimumPasswordLength = 8;

    static readonly string[] SignLanguages = ["LSF", "LSFB", "LSQ", "LSF-SR"];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Compte").RequireRateLimiting("auth");
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);

        var me = app.MapGroup("/api/me").WithTags("Compte").RequireAuthorization();
        me.MapGet("/", GetMeAsync);
        me.MapPut("/preferences", UpdatePreferencesAsync);
    }

    static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    static async Task<IResult> RegisterAsync(RegisterRequest request, VoxSignDbContext db, IPasswordHasher<User> hasher, TokenService tokens, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Trim().Length > 80)
            errors["firstName"] = ["Le prénom est obligatoire (80 caractères au plus)."];
        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            errors["email"] = ["Adresse e-mail invalide."];
        if (request.Password is null || request.Password.Length < MinimumPasswordLength)
            errors["password"] = [$"Le mot de passe doit compter au moins {MinimumPasswordLength} caractères."];
        if (!SignLanguages.Contains(request.SignLanguage))
            errors["signLanguage"] = [$"Langue des signes inconnue ({string.Join(", ", SignLanguages)})."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Results.Problem(title: "Un compte existe déjà avec cette adresse e-mail.", statusCode: StatusCodes.Status409Conflict);

        var user = new User { FirstName = request.FirstName.Trim(), Email = email, SignLanguage = request.SignLanguage };
        user.PasswordHash = hasher.HashPassword(user, request.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var (token, expiresAt) = tokens.Create(user);
        return Results.Created("/api/me", new AuthResponse(token, expiresAt, user.ToDto()));
    }

    static async Task<IResult> LoginAsync(LoginRequest request, VoxSignDbContext db, IPasswordHasher<User> hasher, TokenService tokens, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email ?? string.Empty);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var result = user is null ? PasswordVerificationResult.Failed : hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? string.Empty);
        if (user is null || result == PasswordVerificationResult.Failed)
            return Results.Problem(title: "Adresse e-mail ou mot de passe incorrect.", statusCode: StatusCodes.Status401Unauthorized);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password!);
            await db.SaveChangesAsync(ct);
        }

        var (token, expiresAt) = tokens.Create(user);
        return Results.Ok(new AuthResponse(token, expiresAt, user.ToDto()));
    }

    static async Task<IResult> GetMeAsync(ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        return user is null ? Results.Unauthorized() : Results.Ok(user.ToDto());
    }

    static async Task<IResult> UpdatePreferencesAsync(UpdatePreferencesRequest request, ClaimsPrincipal principal, VoxSignDbContext db, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.UserId()], ct);
        if (user is null)
            return Results.Unauthorized();

        if (request.SignLanguage is { } language)
        {
            if (!SignLanguages.Contains(language))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["signLanguage"] = ["Langue des signes inconnue."] });
            user.SignLanguage = language;
        }

        if (request.Speech is { } speech)
        {
            if (speech.Pitch is < 0.5f or > 2f || speech.Volume is < 0f or > 1f || string.IsNullOrWhiteSpace(speech.Locale))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["speech"] = ["Hauteur entre 0,5 et 2, volume entre 0 et 1, langue obligatoire."] });
            user.SpeechLocale = speech.Locale;
            user.SpeechPitch = speech.Pitch;
            user.SpeechVolume = speech.Volume;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(user.ToDto());
    }
}
