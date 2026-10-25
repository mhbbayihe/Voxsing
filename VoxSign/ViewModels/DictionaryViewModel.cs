using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;

namespace VoxSign.ViewModels;

public partial class DictionaryViewModel(INavigationService navigation, IContentService content, IFavoritesService favorites, IDialogService dialogs)
    : BaseViewModel(navigation)
{
    const string AllKey = "";

    [ObservableProperty]
    string searchText = string.Empty;

    [ObservableProperty]
    string catalogCount = string.Empty;

    [ObservableProperty]
    string offlineCount = string.Empty;

    [ObservableProperty]
    bool offlineOnly;

    public ObservableCollection<FilterOption> Filters { get; } = [];

    public ObservableCollection<Sign> Signs { get; } = [];

    public override async Task LoadAsync()
    {
        if (Filters.Count == 0)
        {
            Filters.Add(new FilterOption("Tous", AllKey) { IsSelected = true });
            foreach (var category in await content.GetCategoriesAsync())
                Filters.Add(new FilterOption(category.ShortLabel, category.Key));
        }

        var total = await content.GetCatalogSizeAsync();
        var offline = await content.GetOfflineSignCountAsync();
        CatalogCount = $"{total} repère{(total > 1 ? "s" : "")}";
        OfflineCount = $"{offline} repère{(offline > 1 ? "s" : "")} disponible{(offline > 1 ? "s" : "")} hors connexion";
        await ApplyAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = ApplyAsync();

    async Task ApplyAsync()
    {
        var filter = Filters.FirstOrDefault(f => f.IsSelected)?.Key ?? AllKey;
        var signs = await content.GetSignsAsync(filter, SearchText);
        Signs.Clear();
        foreach (var sign in signs.Where(s => !OfflineOnly || s.IsOffline))
            Signs.Add(sign);
    }

    [RelayCommand]
    Task SelectFilterAsync(FilterOption option)
    {
        foreach (var f in Filters)
            f.IsSelected = f == option;
        return ApplyAsync();
    }

    [RelayCommand]
    Task OpenSignAsync(Sign sign) =>
        Navigation.GoToAsync(Routes.SignDetail, new Dictionary<string, object> { ["id"] = sign.Id });

    [RelayCommand]
    async Task ToggleFavoriteAsync(Sign sign)
    {
        sign.IsFavorite = !sign.IsFavorite;
        await favorites.SetAsync(FavoriteRecord.SignKind, sign.Id, sign.IsFavorite);
    }

    [RelayCommand]
    async Task ShowFiltersAsync()
    {
        var choice = await dialogs.ChooseAsync("Filtres", OfflineOnly ? "Tous les repères" : "Repères hors connexion uniquement");
        if (choice is null)
            return;
        OfflineOnly = !OfflineOnly;
        await ApplyAsync();
    }

    [RelayCommand]
    Task OpenOfflineAsync() => Navigation.GoToAsync(Routes.Offline);
}
