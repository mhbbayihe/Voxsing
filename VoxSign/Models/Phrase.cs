using CommunityToolkit.Mvvm.ComponentModel;

namespace VoxSign.Models;

/// <summary>Une phrase courante (catalogue ou phrase personnelle), prête à être montrée, lue ou illustrée.</summary>
public partial class Phrase : ObservableObject
{
    public required string Id { get; init; }

    public required string Text { get; init; }

    public required string ThemeKey { get; init; }

    /// <summary>Thème affiché (« Au café », « Santé »…).</summary>
    public required string Theme { get; init; }

    /// <summary>Intention (« Commander », « Payer »…).</summary>
    public required string Intent { get; init; }

    /// <summary>Contexte affiché dans les favoris (« Me présenter ») ; à défaut, le thème.</summary>
    public string? Context { get; init; }

    public bool IsCustom { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Caption))]
    bool isFavorite;

    /// <summary>Ligne grise sous la phrase dans la liste « Phrases utiles ».</summary>
    public string Caption => $"{Intent} · {(IsFavorite ? "Dans vos favoris" : "Illustrer ou lire")}";

    /// <summary>Ligne grise sous la phrase dans « Vos favoris ».</summary>
    public string SavedCaption => $"{Context ?? Theme} · Enregistrée";
}
