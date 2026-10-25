namespace VoxSign.Models;

public enum ChatMessageKind
{
    /// <summary>Message texte reçu.</summary>
    Received,
    /// <summary>Phrase envoyée avec sa séquence de repères illustrés.</summary>
    IllustratedSent,
    /// <summary>Repère unique envoyé (vignette + mot).</summary>
    SignSent,
}

public class ChatMessage
{
    public required ChatMessageKind Kind { get; init; }

    public required string Text { get; init; }

    /// <summary>Ligne d'information (« Alex · 9:38 »).</summary>
    public required string Meta { get; init; }

    /// <summary>Repères affichés sous une phrase illustrée, ou le repère d'un <see cref="ChatMessageKind.SignSent"/>.</summary>
    public IReadOnlyList<Sign> Signs { get; init; } = [];

    public Sign? FirstSign => Signs.Count > 0 ? Signs[0] : null;
}
