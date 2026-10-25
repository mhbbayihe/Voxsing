using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.ViewModels;

public partial class PhrasesViewModel(INavigationService navigation, IContentService content, IFavoritesService favorites, IDialogService dialogs)
    : BaseViewModel(navigation)
{
    // Thèmes présentés en filtres ; les autres sont proposés sous la rangée de filtres
    const int SegmentCount = 3;
    // « Pour commencer » montre les premières phrases, « Voir tout » les affiche toutes
    const int PreviewCount = 4;

    IReadOnlyList<ThemeDto> _themes = [];
    string _themeKey = "cafe";

    [ObservableProperty]
    string searchText = string.Empty;

    [ObservableProperty]
    string themeTitle = string.Empty;

    [ObservableProperty]
    string themeDescription = string.Empty;

    [ObservableProperty]
    string themeCount = string.Empty;

    [ObservableProperty]
    string otherThemesText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAllText))]
    bool showAll;

    public string ShowAllText => ShowAll ? "Réduire" : "Voir tout";

    public ObservableCollection<FilterOption> Themes { get; } = [];

    public ObservableCollection<Phrase> Phrases { get; } = [];

    public override async Task LoadAsync()
    {
        _themes = await content.GetThemesAsync();
        if (Themes.Count == 0)
            foreach (var theme in _themes.Take(SegmentCount))
                Themes.Add(new FilterOption(theme.Label, theme.Key) { IsSelected = theme.Key == _themeKey });
        OtherThemesText = string.Join(" · ", _themes.Skip(SegmentCount).Select(t => t.Label));
        await ApplyAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = ApplyAsync();

    partial void OnShowAllChanged(bool value) => _ = ApplyAsync();

    async Task ApplyAsync()
    {
        if (_themes.FirstOrDefault(t => t.Key == _themeKey) is { } theme)
        {
            ThemeTitle = theme.Title;
            ThemeDescription = theme.Description;
            ThemeCount = $"{theme.PhraseCount} phrase{(theme.PhraseCount > 1 ? "s" : "")}";
        }

        // Une recherche porte sur toutes les phrases, un thème sur les siennes
        var phrases = string.IsNullOrWhiteSpace(SearchText)
            ? await content.GetPhrasesAsync(_themeKey)
            : await content.GetPhrasesAsync(search: SearchText);
        Phrases.Clear();
        foreach (var phrase in ShowAll || !string.IsNullOrWhiteSpace(SearchText) ? phrases : phrases.Take(PreviewCount))
            Phrases.Add(phrase);
    }

    [RelayCommand]
    Task SelectThemeAsync(FilterOption option)
    {
        _themeKey = option.Key;
        foreach (var t in Themes)
            t.IsSelected = t == option;
        return ApplyAsync();
    }

    [RelayCommand]
    async Task ShowOtherThemesAsync()
    {
        var others = _themes.Skip(SegmentCount).ToList();
        if (await dialogs.ChooseAsync("Autres thèmes", others.Select(t => t.Label).ToArray()) is not { } label)
            return;

        _themeKey = others.First(t => t.Label == label).Key;
        foreach (var t in Themes)
            t.IsSelected = false;
        await ApplyAsync();
    }

    [RelayCommand]
    void ToggleShowAll() => ShowAll = !ShowAll;

    [RelayCommand]
    Task OpenPhraseAsync(Phrase phrase) =>
        Navigation.GoToAsync(Routes.TextToSign, new Dictionary<string, object> { ["text"] = phrase.Text });

    [RelayCommand]
    async Task ToggleFavoriteAsync(Phrase phrase)
    {
        phrase.IsFavorite = !phrase.IsFavorite;
        await favorites.SetAsync(FavoriteRecord.PhraseKind, phrase.Id, phrase.IsFavorite);
    }

    [RelayCommand]
    Task CreatePhraseAsync() =>
        Navigation.GoToAsync(Routes.TextToSign, new Dictionary<string, object> { ["text"] = string.Empty });

    [RelayCommand]
    async Task ShowOptionsAsync()
    {
        if (await dialogs.ChooseAsync("Options", ShowAll ? "Afficher les premières phrases" : "Afficher toutes les phrases") is not null)
            ShowAll = !ShowAll;
    }
}
