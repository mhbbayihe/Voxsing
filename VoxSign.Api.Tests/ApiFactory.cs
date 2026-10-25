using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using VoxSign.Api.Data;
using VoxSign.Shared;

namespace VoxSign.Api.Tests;

/// <summary>API complète sur une base SQLite en mémoire, propre à chaque classe de tests.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("RateLimit:AuthPerMinute", "10000");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VoxSignDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VoxSignDbContext>>();
            services.AddDbContext<VoxSignDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Client authentifié avec un compte neuf.</summary>
    public async Task<HttpClient> CreateUserClientAsync(string? email = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Camille", email ?? $"{Guid.NewGuid():N}@exemple.fr", "motdepasse-solide", "LSF"));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(descriptor);
    }
}
