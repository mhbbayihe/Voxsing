namespace VoxSign.Services;

/// <summary>
/// Reconnaissance simulée qui « dicte » la phrase de la maquette mot par mot.
/// À remplacer par une implémentation native (SpeechRecognizer Android, SFSpeechRecognizer iOS).
/// </summary>
public class SimulatedSpeechToTextService : ISpeechToTextService
{
    const string Sample = "Bonjour ! Vous souhaitez votre café sur place ou à emporter ?";

    CancellationTokenSource? _cts;

    public event EventHandler<string>? TranscriptUpdated;

    public event EventHandler<double>? LevelChanged;

    public bool IsListening => _cts is not null;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _cts = null;
        LevelChanged?.Invoke(this, 0);
        return Task.CompletedTask;
    }

    async Task RunAsync(CancellationToken token)
    {
        var words = Sample.Split(' ');
        var random = new Random();
        try
        {
            for (var i = 1; i <= words.Length; i++)
            {
                for (var tick = 0; tick < 3; tick++)
                {
                    await Task.Delay(100, token);
                    LevelChanged?.Invoke(this, 0.35 + random.NextDouble() * 0.65);
                }

                TranscriptUpdated?.Invoke(this, string.Join(' ', words.Take(i)));
            }

            while (true)
            {
                await Task.Delay(100, token);
                LevelChanged?.Invoke(this, random.NextDouble() * 0.3);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
