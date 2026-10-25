using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;

namespace VoxSign.Api.Services;

/// <summary>Taille de téléchargement d'un pack : images des repères + contenu JSON (estimé).</summary>
public class PackSizer(IWebHostEnvironment environment)
{
    const long JsonBytesPerItem = 600;

    readonly ConcurrentDictionary<string, long> _imageSizes = new();

    public long Estimate(IEnumerable<string> imageKeys, int itemCount) =>
        imageKeys.Distinct().Sum(ImageSize) + itemCount * JsonBytesPerItem;

    long ImageSize(string imageKey) => _imageSizes.GetOrAdd(imageKey, key =>
    {
        IFileInfo file = environment.WebRootFileProvider.GetFileInfo($"{Mapping.SignImagesPath.TrimStart('/')}/{key}.jpg");
        return file.Exists ? file.Length : 0;
    });
}
