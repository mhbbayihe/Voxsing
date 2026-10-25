namespace VoxSign.Services;

/// <summary>Reconnaissance vocale en continu.</summary>
public interface ISpeechToTextService
{
    /// <summary>Le texte reconnu jusqu'ici, à chaque mise à jour.</summary>
    event EventHandler<string>? TranscriptUpdated;

    /// <summary>Niveau sonore normalisé (0 à 1), environ 10 fois par seconde.</summary>
    event EventHandler<double>? LevelChanged;

    bool IsListening { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync();
}
