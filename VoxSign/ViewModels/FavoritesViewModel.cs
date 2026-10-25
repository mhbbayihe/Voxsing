using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    readonly IContentService _content;
    readonly IFavoritesService _favorites;
    readonly IDialogService _dialogs;
    string _filter = "all";

    public FavoritesViewModel(INavigationService navigation, IContentService content, IFavoritesService favorites, IDialogService dialogs, ISyncService sync)
        : base(navigation)
    {
        _content = content;
        _favorites = favorites;
        _dialogs = dialogs;
        // Favoris reçus du serveur (autre appareil, retour du réseau) : on rafraîchit la liste
        sync.Synchronized += (_, _) => MainThread.BeginInvokeOnMainThread(OnAppearing);
    }

    [ObservableProperty]
    string searchText = string.Empty;

    [ObservableProperty]
    bool showSigns = true;

    [ObservableProperty]
    bool showPhrases = true;

    [ObservableProperty]
    bool offlineOnly;

    public ObservableCollection<FilterOption> Filters { get; } = [];

    public ObservableCollection<Sign> Signs { get; } = [];

    public ObservableCollection<Phrase> Phrases { get; } = [];

    public string SignsCount => $"{Signs.Count} enregistré{(Signs.Count > 1 ? "s" : "")}";

    public string PhrasesCount => $"{Phrases.Count} phrase{(Phrases.Count > 1 ? "s" : "")}";

    partial void OnSearchTextChanged(string value) => OnAppearing();

    public override async Task LoadAsync()
    {
        bool Matches(string text) => string.IsNullOrWhiteSpace(SearchText) || TextSearch.Contains(text, SearchText);

        var signs = (await _content.GetFavoriteSignsAsync()).Where(s => Matches(s.Word) && (!OfflineOnly || s.IsOffline)).ToList();
        var phrases = (await _content.GetFavoritePhrasesAsync()).Where(p => Matches(p.Text)).ToList();

        Signs.Clear();
        foreach (var sign in signs)
            Signs.Add(sign);
        Phrases.Clear();
        foreach (var phrase in phrases)
            Phrases.Add(phrase);

        Filters.Clear();
        Filters.Add(new FilterOption($"Tout · {signs.Count + phrases.Count}", "all") { IsSelected = _filter == "all" });
        Filters.Add(new FilterOption($"Signes · {signs.Count}", "signs") { IsSelected = _filter == "signs" });
        Filters.Add(new FilterOption($"Phrases · {phrases.Count}", "phrases") { IsSelected = _filter == "phrases" });
        OnPropertyChanged(nameof(SignsCount));
        OnPropertyChanged(nameof(PhrasesCount));
    }

    [RelayCommand]
    void SelectFilter(FilterOption option)
    {
        _filter = option.Key;
        foreach (var f in Filters)
            f.IsSelected = f == option;
        ShowSigns = option.Key != "phrases";
        ShowPhrases = option.Key != "signs";
    }

    [RelayCommand]
    Task OpenSignAsync(Sign sign) =>
        Navigation.GoToAsync(Routes.SignDetail, new Dictionary<string, object> { ["id"] = sign.Id });

    [RelayCommand]
    Task OpenPhraseAsync(Phrase phrase) =>
        Navigation.GoToAsync(Routes.TextToSign, new Dictionary<string, object> { ["text"] = phrase.Text });

    [RelayCommand]
    async Task RemoveSignAsync(Sign sign)
    {
        await _favorites.SetAsync(FavoriteRecord.SignKind, sign.Id, false);
        await LoadAsync();
    }

    [RelayCommand]
    async Task RemovePhraseAsync(Phrase phrase)
    {
        await _favorites.SetAsync(FavoriteRecord.PhraseKind, phrase.Id, false);
        await LoadAsync();
    }

    [RelayCommand]
    async Task ShowFiltersAsync()
    {
        if (await _dialogs.ChooseAsync("Filtres", OfflineOnly ? "Tous les repères" : "Repères hors ligne uniquement") is null)
            return;
        OfflineOnly = !OfflineOnly;
        await LoadAsync();
    }
}
