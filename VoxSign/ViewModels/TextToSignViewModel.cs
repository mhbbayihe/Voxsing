using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;
using VoxSign.Services.Local;
using VoxSign.Shared;

namespace VoxSign.ViewModels;

public partial class TextToSignViewModel : BaseViewModel, IQueryAttributable
{
    public const int MaxLength = 300;

    // Le temps de finir un mot avant de relancer la traduction
    static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(350);

    readonly IContentService _content;
    readonly IFavoritesService _favorites;
    readonly ISpeechService _speech;
    readonly IDialogService _dialogs;
    CancellationTokenSource? _translation;

    public TextToSignViewModel(INavigationService navigation, IContentService content, IFavoritesService favorites, ISpeechService speech, IDialogService dialogs)
        : base(navigation)
    {
        _content = content;
        _favorites = favorites;
        _speech = speech;
        _dialogs = dialogs;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterCount))]
    [NotifyCanExecuteChangedFor(nameof(IllustrateCommand), nameof(ReadAloudCommand), nameof(AddToFavoritesCommand))]
    string message = "Bonjour, je voudrais un café, s’il vous plaît.";

    public ObservableCollection<Sign> Preview { get; } = [];

    public string CharacterCount => $"{Message.Length} / {MaxLength} caractères";

    bool HasMessage() => !string.IsNullOrWhiteSpace(Message);

    public override Task LoadAsync() => UpdatePreviewAsync(TimeSpan.Zero);

    partial void OnMessageChanged(string value) => _ = UpdatePreviewAsync(TypingDelay);

    async Task UpdatePreviewAsync(TimeSpan delay)
    {
        _translation?.Cancel();
        var cts = _translation = new CancellationTokenSource();
        try
        {
            await Task.Delay(delay, cts.Token);
            // Résultat immédiat sur l'appareil, puis celui du serveur s'il est joignable
            ShowPreview(await _content.TranslateOnDeviceAsync(Message));
            var signs = await _content.TranslateAsync(Message, cts.Token);
            if (!cts.IsCancellationRequested)
                ShowPreview(signs);
        }
        catch (OperationCanceledException)
        {
        }
    }

    void ShowPreview(IReadOnlyList<Sign> signs)
    {
        var distinct = signs.DistinctBy(s => s.Id).ToList();
        if (distinct.Select(s => s.Id).SequenceEqual(Preview.Select(s => s.Id)))
            return;

        Preview.Clear();
        foreach (var sign in distinct)
            Preview.Add(sign);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("text", out var text) && text is string value)
            Message = value;
    }

    [RelayCommand]
    void Clear() => Message = string.Empty;

    [RelayCommand(CanExecute = nameof(HasMessage))]
    Task IllustrateAsync() => OpenSequenceAsync();

    [RelayCommand]
    Task OpenSequenceAsync() => Navigation.GoToAsync(Routes.Sequence, new Dictionary<string, object> { ["text"] = Message });

    [RelayCommand(CanExecute = nameof(HasMessage))]
    Task ReadAloudAsync() => _speech.SpeakAsync(Message);

    /// <summary>Une phrase du catalogue rejoint les favoris ; un texte libre devient d'abord une phrase personnelle.</summary>
    [RelayCommand(CanExecute = nameof(HasMessage))]
    async Task AddToFavoritesAsync()
    {
        var text = Message.Trim();
        var phrase = (await _content.GetPhrasesAsync(search: text)).FirstOrDefault(p => TextSearch.Fold(p.Text) == TextSearch.Fold(text))
                     ?? await _content.SaveCustomPhraseAsync(text);
        await _favorites.SetAsync(FavoriteRecord.PhraseKind, phrase.Id, true);
        await _dialogs.AlertAsync("Favoris", phrase.IsCustom
            ? "La phrase a été enregistrée dans vos phrases et ajoutée à vos favoris."
            : "La phrase a été ajoutée à vos favoris.");
    }
}
