using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;

namespace VoxSign.ViewModels;

public partial class SequenceViewModel(INavigationService navigation, IContentService content, IFavoritesService favorites, IShare share)
    : BaseViewModel(navigation), IQueryAttributable
{
    string? _loadedPhrase;

    [ObservableProperty]
    string phrase = "Bonjour, je voudrais un café, s’il vous plaît.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Position))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand), nameof(NextCommand))]
    SequenceStep? current;

    public ObservableCollection<SequenceStep> Steps { get; } = [];

    public string Position => Current is null ? string.Empty : $"Repère {Current.Number} sur {Steps.Count}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("text", out var text) && text is string value)
            Phrase = value;
    }

    public override async Task LoadAsync()
    {
        if (_loadedPhrase == Phrase)
            return;

        _loadedPhrase = Phrase;
        var signs = await content.TranslateAsync(Phrase);
        Steps.Clear();
        for (var i = 0; i < signs.Count; i++)
            Steps.Add(new SequenceStep(i + 1, signs[i]));

        // La maquette ouvre la séquence sur son deuxième repère
        Select(Steps.ElementAtOrDefault(Steps.Count > 1 ? 1 : 0));
    }

    [RelayCommand]
    void Select(SequenceStep? step)
    {
        foreach (var s in Steps)
            s.IsCurrent = s == step;
        Current = step;
    }

    bool CanGoPrevious() => Current is not null && Current.Number > 1;

    bool CanGoNext() => Current is not null && Current.Number < Steps.Count;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    void Previous() => Select(Steps[Current!.Number - 2]);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    void Next() => Select(Steps[Current!.Number]);

    [RelayCommand]
    async Task ToggleFavoriteAsync()
    {
        if (Current?.Sign is not { } sign)
            return;
        sign.IsFavorite = !sign.IsFavorite;
        await favorites.SetAsync(FavoriteRecord.SignKind, sign.Id, sign.IsFavorite);
    }

    [RelayCommand]
    Task OpenSignAsync() => Current is null
        ? Task.CompletedTask
        : Navigation.GoToAsync(Routes.SignDetail, new Dictionary<string, object> { ["id"] = Current.Sign.Id });

    [RelayCommand]
    Task ShareAsync() => share.RequestAsync(new ShareTextRequest
    {
        Title = "Partager le support",
        Text = $"{Phrase}\nRepères LSF : {string.Join(" · ", Steps.Select(s => s.Sign.Word))}",
    });
}
