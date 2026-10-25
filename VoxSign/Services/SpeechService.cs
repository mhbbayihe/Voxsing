using VoxSign.Services.Api;
using VoxSign.Shared;

namespace VoxSign.Services;

public interface ISpeechService
{
    bool IsSpeaking { get; }

    /// <summary>Lit le texte avec la voix de l'appareil (disponible hors ligne), selon les préférences du compte.</summary>
    Task SpeakAsync(string text);

    void Stop();
}

/// <summary>
/// Synthèse vocale sur l'appareil : elle fonctionne sans réseau avec les voix déjà installées.
/// Les préférences (langue, hauteur, volume) sont celles du compte, conservées par le serveur.
/// </summary>
public class SpeechService(ITextToSpeech textToSpeech, ISessionStore session) : ISpeechService
{
    CancellationTokenSource? _current;
    IReadOnlyList<Locale>? _locales;

    public bool IsSpeaking => _current is not null;

    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Stop();
        var cts = _current = new CancellationTokenSource();
        var preferences = session.User?.Speech ?? SpeechPreferencesDto.Default;
        try
        {
            await textToSpeech.SpeakAsync(text, new SpeechOptions
            {
                Locale = await FindLocaleAsync(preferences.Locale),
                Pitch = preferences.Pitch,
                Volume = preferences.Volume,
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_current == cts)
                _current = null;
            cts.Dispose();
        }
    }

    public void Stop()
    {
        _current?.Cancel();
        _current = null;
    }

    /// <summary>Voix exacte (« fr-BE »), sinon une voix de la même langue (« fr »), sinon la voix par défaut.</summary>
    async Task<Locale?> FindLocaleAsync(string tag)
    {
        _locales ??= (await textToSpeech.GetLocalesAsync()).ToList();
        var parts = tag.Split('-');
        return _locales.FirstOrDefault(l => string.Equals(l.Language, parts[0], StringComparison.OrdinalIgnoreCase)
                                            && parts.Length > 1 && string.Equals(l.Country, parts[1], StringComparison.OrdinalIgnoreCase))
               ?? _locales.FirstOrDefault(l => l.Language.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase));
    }
}
