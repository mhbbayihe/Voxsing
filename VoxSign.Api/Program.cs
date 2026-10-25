using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoxSign.Api.Data;
using VoxSign.Api.Endpoints;
using VoxSign.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Données
builder.Services.AddDbContext<VoxSignDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("VoxSign") ?? "Data Source=voxsign.db"));

// Authentification par jeton (JWT)
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Section))
    .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey doit compter au moins 32 caractères.")
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        bearer.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = jwt.Value.Key,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<PackSizer>();

// Limite les tentatives de connexion et d'inscription (par adresse IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimit:AuthPerMinute", 10),
            Window = TimeSpan.FromMinutes(1),
        }));
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseStaticFiles(); // wwwroot/media/signs : images des repères
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Supervision");
app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapTranslateEndpoints();
app.MapFavoritesEndpoints();
app.MapPackEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbSeeder.SeedAsync(
        services.GetRequiredService<VoxSignDbContext>(),
        services.GetRequiredService<IPasswordHasher<User>>(),
        includeDemoAccount: app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:DemoAccount"));
}

app.Run();

public partial class Program;
