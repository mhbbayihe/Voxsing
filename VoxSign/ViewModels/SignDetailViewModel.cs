using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;

namespace VoxSign.ViewModels;

public partial class SignDetailViewModel(
    INavigationService navigation, IContentService content, IFavoritesService favorites, ISpeechService speech, IShare share)
    : BaseViewModel(navigation), IQueryAttributable
{
    // Repère affiché par défaut (celui de la maquette)
    string _signId = "merci";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle), nameof(ReadLabel))]
    Sign? sign;

    public string Subtitle => Sign is null ? string.Empty : $"Dictionnaire · {Sign.Category}";

    public string ReadLabel => Sign is null ? string.Empty : $"Lire le mot « {Sign.Word} »";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string value)
            _signId = value;
    }

    public override async Task LoadAsync() => Sign = await content.GetSignAsync(_signId);

    [RelayCommand]
    async Task ToggleFavoriteAsync()
    {
        if (Sign is null)
            return;
        Sign.IsFavorite = !Sign.IsFavorite;
        await favorites.SetAsync(FavoriteRecord.SignKind, Sign.Id, Sign.IsFavorite);
    }

    [RelayCommand]
    Task ReadWordAsync() => Sign is null ? Task.CompletedTask : speech.SpeakAsync(Sign.Word);

    [RelayCommand]
    Task ShareAsync() => Sign is null
        ? Task.CompletedTask
        : share.RequestAsync(new ShareTextRequest { Title = Sign.Word, Text = $"{Sign.Word} en LSF · {Sign.Usage}" });
}
