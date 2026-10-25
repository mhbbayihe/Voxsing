using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Services;

namespace VoxSign.ViewModels;

public partial class VoiceToTextViewModel : BaseViewModel
{
    // Hauteurs des 13 barres de l'onde dans la maquette (de gauche à droite)
    static readonly double[] RestingLevels = [10, 18, 28, 40, 24, 34, 44, 22, 32, 16, 24, 12, 8];

    readonly ISpeechToTextService _speech;
    readonly IClipboard _clipboard;
    readonly IDispatcherTimer _timer;
    readonly Random _random = new();
    DateTime _startedAt;

    public VoiceToTextViewModel(INavigationService navigation, ISpeechToTextService speech, IClipboard clipboard, IDispatcher dispatcher)
        : base(navigation)
    {
        _speech = speech;
        _clipboard = clipboard;
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Elapsed = DateTime.Now - _startedAt;

        foreach (var level in RestingLevels)
            Levels.Add(level);

        _speech.TranscriptUpdated += (_, text) => MainThread.BeginInvokeOnMainThread(() => Transcript = text);
        _speech.LevelChanged += (_, level) => MainThread.BeginInvokeOnMainThread(() => AnimateLevels(level));
    }

    /// <summary>Texte d'exemple de la maquette, remplacé dès que l'écoute produit du texte.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand), nameof(ReplyCommand))]
    string transcript = "Bonjour ! Vous souhaitez votre café sur place ou à emporter ?";

    /// <summary>État affiché ; l'écran s'ouvre en écoute (voir <see cref="StartAsync"/>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(ToggleText))]
    bool isListening = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    TimeSpan elapsed = TimeSpan.FromSeconds(18);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TranscriptPitch))]
    double transcriptSize = 26;

    /// <summary>Hauteurs courantes des barres de l'onde.</summary>
    public ObservableCollection<double> Levels { get; } = [];

    public string StatusText => IsListening ? $"Le micro est actif · {Elapsed:mm\\:ss}" : $"Micro en pause · {Elapsed:mm\\:ss}";

    public string ToggleText => IsListening ? "Arrêter l’écoute" : "Reprendre l’écoute";

    public double TranscriptPitch => Math.Round(TranscriptSize * 38 / 26);

    bool HasTranscript() => !string.IsNullOrWhiteSpace(Transcript);

    /// <summary>Démarre l'écoute à l'ouverture de l'écran.</summary>
    public async Task StartAsync()
    {
        if (_speech.IsListening)
            return;

        Transcript = string.Empty;
        _startedAt = DateTime.Now;
        Elapsed = TimeSpan.Zero;
        await _speech.StartAsync();
        IsListening = true;
        _timer.Start();
    }

    public async Task StopAsync()
    {
        _timer.Stop();
        await _speech.StopAsync();
        IsListening = false;
    }

    void AnimateLevels(double level)
    {
        for (var i = 0; i < Levels.Count; i++)
        {
            var target = Math.Max(8, RestingLevels[i] * (0.4 + level * (0.6 + _random.NextDouble() * 0.4)));
            Levels[i] = Math.Round(target / 2) * 2;
        }
    }

    [RelayCommand]
    Task ToggleListeningAsync() => IsListening ? StopAsync() : StartAsync();

    [RelayCommand]
    void Enlarge() => TranscriptSize = TranscriptSize >= 34 ? 26 : TranscriptSize + 4;

    [RelayCommand(CanExecute = nameof(HasTranscript))]
    Task CopyAsync() => _clipboard.SetTextAsync(Transcript);

    [RelayCommand(CanExecute = nameof(HasTranscript))]
    async Task ReplyAsync()
    {
        await StopAsync();
        await Navigation.GoToAsync(Routes.TextToSign, new Dictionary<string, object> { ["text"] = string.Empty });
    }
}
