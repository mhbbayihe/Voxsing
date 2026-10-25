using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoxSign.Models;

/// <summary>Un pack de repères et de phrases consultable sans connexion.</summary>
public partial class OfflinePack : ObservableObject
{
    static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Nom d'icône (voir <see cref="Controls.Icons"/>).</summary>
    public required string Icon { get; init; }

    public int SignCount { get; init; }

    public int PhraseCount { get; init; }

    public long SizeBytes { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(StatusIcon), nameof(ActionText))]
    bool isDownloaded;

    /// <summary>Une version plus récente est disponible sur le serveur.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    bool hasUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    bool isBusy;

    public string Content => $"{SignCount} signes · {PhraseCount} phrases";

    public string Size => FormatSize(SizeBytes);

    public string StatusText => IsDownloaded ? $"Téléchargé · {Size}" : Size;

    public string StatusIcon => IsDownloaded ? "CheckCircleTiny" : "DownloadSmall";

    public string ActionText => IsBusy ? "Patientez…" : HasUpdate ? "Mettre à jour" : IsDownloaded ? "Gérer" : "Télécharger";

    public static string FormatSize(long bytes)
    {
        var megabytes = bytes / 1_000_000d;
        return megabytes < 0.1 ? "< 0,1 Mo" : $"{megabytes.ToString(megabytes < 10 ? "0.#" : "0", French)} Mo";
    }
}
