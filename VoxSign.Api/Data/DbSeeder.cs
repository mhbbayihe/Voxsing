using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoxSign.Shared;

namespace VoxSign.Api.Data;

public static class DbSeeder
{
    /// <summary>Crée la base si besoin et y insère le catalogue initial (une seule fois).</summary>
    public static async Task SeedAsync(VoxSignDbContext db, IPasswordHasher<User> hasher, bool includeDemoAccount, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Categories.AnyAsync(ct))
        {
            db.Categories.AddRange(SeedCatalog.Categories.Select((c, i) => new Category { Key = c.Key, Label = c.Label, ShortLabel = c.ShortLabel, Order = i }));
            db.Themes.AddRange(SeedCatalog.Themes.Select((t, i) => new Theme { Key = t.Key, Label = t.Label, Title = t.Title, Description = t.Description, Order = i }));
            db.Signs.AddRange(SeedCatalog.Signs.Select(s => new Sign
            {
                Id = s.Id,
                Word = s.Word,
                CategoryKey = s.CategoryKey,
                ImageKey = s.ImageKey,
                Usage = s.Usage,
                UsageDescription = s.UsageDescription,
                Example = s.Example,
                Keywords = [.. s.Keywords],
            }));
            db.Phrases.AddRange(SeedCatalog.Phrases.Select((p, i) => new Phrase
            {
                Id = p.Id,
                Text = p.Text,
                ThemeKey = p.ThemeKey,
                Intent = p.Intent,
                Context = p.Context,
                Order = i,
            }));
            db.Packs.AddRange(SeedCatalog.Packs.Select((p, i) => new Pack
            {
                Id = p.Id,
                Name = p.Name,
                Icon = p.Icon,
                Version = p.Version,
                Order = i,
                Signs = [.. p.SignIds.Select((id, order) => new PackSign { PackId = p.Id, SignId = id, Order = order })],
                Phrases = [.. p.PhraseIds.Select((id, order) => new PackPhrase { PackId = p.Id, PhraseId = id, Order = order })],
            }));
            await db.SaveChangesAsync(ct);
        }

        if (includeDemoAccount)
        {
            var email = SeedCatalog.DemoAccount.Email;
            if (!await db.Users.AnyAsync(u => u.Email == email, ct))
            {
                var user = new User { FirstName = SeedCatalog.DemoAccount.FirstName, Email = email };
                user.PasswordHash = hasher.HashPassword(user, SeedCatalog.DemoAccount.Password);
                user.FavoriteSigns = [.. SeedCatalog.DemoAccount.FavoriteSignIds.Select(id => new FavoriteSign { UserId = user.Id, SignId = id })];
                user.FavoritePhrases = [.. SeedCatalog.DemoAccount.FavoritePhraseIds.Select(id => new FavoritePhrase { UserId = user.Id, PhraseId = id })];
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
