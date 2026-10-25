using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Api;

namespace VoxSign.ViewModels;

public partial class OfflineViewModel(INavigationService navigation, IOfflinePackService packs, IDialogService dialogs, IConnectivity connectivity)
    : BaseViewModel(navigation)
{
    const string RemoveAction = "Supprimer de l’appareil";
    const string UpdateAction = "Mettre à jour";

    [ObservableProperty]
    string summary = string.Empty;

    [ObservableProperty]
    string details = string.Empty;

    /// <summary>Part du catalogue de packs présente sur l'appareil (0 à 1).</summary>
    [ObservableProperty]
    double usage;

    public ObservableCollection<OfflinePack> Downloaded { get; } = [];

    public ObservableCollection<OfflinePack> Available { get; } = [];

    /// <summary>Affiche aussitôt les packs de l'appareil, puis la liste à jour du serveur quand elle arrive.</summary>
    public override async Task LoadAsync()
    {
        Show(await packs.GetPacksAsync());
        Show(await packs.GetPacksAsync(refresh: true));
    }

    void Show(IReadOnlyList<OfflinePack> all)
    {
        Downloaded.Clear();
        Available.Clear();
        foreach (var pack in all)
            (pack.IsDownloaded ? Downloaded : Available).Add(pack);

        var count = Downloaded.Count;
        var used = Downloaded.Sum(p => p.SizeBytes);
        Summary = $"{count} pack{(count > 1 ? "s" : "")} prêt{(count > 1 ? "s" : "")} à vous accompagner";
        Details = $"{Downloaded.Sum(p => p.SignCount)} signes · {Downloaded.Sum(p => p.PhraseCount)} phrases · {OfflinePack.FormatSize(used)} utilisés";
        var total = all.Sum(p => p.SizeBytes);
        Usage = total == 0 ? 0 : (double)used / total;
    }

    [RelayCommand]
    async Task PackActionAsync(OfflinePack pack)
    {
        if (pack.IsBusy)
            return;

        var action = !pack.IsDownloaded
            ? UpdateAction
            : await dialogs.ChooseAsync(pack.Name, pack.HasUpdate ? [UpdateAction, RemoveAction] : [RemoveAction]);
        if (action is null)
            return;

        if (action == UpdateAction && connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            await dialogs.AlertAsync(pack.Name, "Le téléchargement nécessite une connexion Internet (de préférence en Wi-Fi).");
            return;
        }

        pack.IsBusy = true;
        try
        {
            if (action == UpdateAction)
                await packs.DownloadAsync(pack);
            else
                await packs.RemoveAsync(pack);
            Show(await packs.GetPacksAsync());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException or IOException)
        {
            await dialogs.AlertAsync(pack.Name, "Le téléchargement n’a pas abouti. Vérifiez votre connexion et réessayez.");
        }
        finally
        {
            pack.IsBusy = false;
        }
    }

    [RelayCommand]
    Task ShowOptionsAsync() => dialogs.AlertAsync("Hors connexion",
        "Les packs téléchargés restent consultables sans réseau : repères illustrés, phrases et lecture vocale avec les voix de votre téléphone.");
}
