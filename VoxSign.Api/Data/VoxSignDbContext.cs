using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace VoxSign.Api.Data;

public class VoxSignDbContext(DbContextOptions<VoxSignDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sign> Signs => Set<Sign>();
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<Phrase> Phrases => Set<Phrase>();
    public DbSet<Pack> Packs => Set<Pack>();
    public DbSet<FavoriteSign> FavoriteSigns => Set<FavoriteSign>();
    public DbSet<FavoritePhrase> FavoritePhrases => Set<FavoritePhrase>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirstName).HasMaxLength(80);
            e.Property(u => u.Email).HasMaxLength(254);
            e.HasMany(u => u.FavoriteSigns).WithOne().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.FavoritePhrases).WithOne().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Category>().HasKey(c => c.Key);
        model.Entity<Theme>().HasKey(t => t.Key);

        model.Entity<Sign>(e =>
        {
            // Liste de mots-clés stockée en JSON dans une colonne texte
            e.Property(s => s.Keywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToList()));
            e.HasOne(s => s.Category).WithMany().HasForeignKey(s => s.CategoryKey);
        });

        model.Entity<Phrase>(e =>
        {
            e.Property(p => p.Text).HasMaxLength(300);
            e.HasOne(p => p.Theme).WithMany().HasForeignKey(p => p.ThemeKey);
            e.HasIndex(p => p.OwnerId);
        });

        model.Entity<PackSign>(e =>
        {
            e.HasKey(p => new { p.PackId, p.SignId });
            e.HasOne(p => p.Sign).WithMany().HasForeignKey(p => p.SignId);
        });
        model.Entity<PackPhrase>(e =>
        {
            e.HasKey(p => new { p.PackId, p.PhraseId });
            e.HasOne(p => p.Phrase).WithMany().HasForeignKey(p => p.PhraseId);
        });
        model.Entity<Pack>(e =>
        {
            e.HasMany(p => p.Signs).WithOne().HasForeignKey(p => p.PackId);
            e.HasMany(p => p.Phrases).WithOne().HasForeignKey(p => p.PackId);
        });

        model.Entity<FavoriteSign>(e =>
        {
            e.HasKey(f => new { f.UserId, f.SignId });
            e.HasOne<Sign>().WithMany().HasForeignKey(f => f.SignId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<FavoritePhrase>(e =>
        {
            e.HasKey(f => new { f.UserId, f.PhraseId });
            e.HasOne<Phrase>().WithMany().HasForeignKey(f => f.PhraseId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
