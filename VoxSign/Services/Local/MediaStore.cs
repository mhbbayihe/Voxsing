using VoxSign.Shared;

namespace VoxSign.Services.Local;

/// <summary>Images des repères : fichiers téléchargés avec les packs, ou images embarquées dans l'application.</summary>
public static class MediaStore
{
    static readonly HashSet<string> BundledKeys = SeedCatalog.Signs.Select(s => s.ImageKey).ToHashSet();

    public static string Directory => Path.Combine(FileSystem.AppDataDirectory, "media", "signs");

    public static string LocalPath(string imageKey) => Path.Combine(Directory, $"{imageKey}.jpg");

    public static bool IsBundled(string imageKey) => BundledKeys.Contains(imageKey);

    /// <summary>Source affichable : fichier local, puis image embarquée, puis adresse web.</summary>
    public static string Resolve(string imageKey, string imageUrl) =>
        File.Exists(LocalPath(imageKey)) ? LocalPath(imageKey)
        : IsBundled(imageKey) ? $"sign_{imageKey}.jpg"
        : imageUrl;
}
