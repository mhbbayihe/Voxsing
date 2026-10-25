using CommunityToolkit.Mvvm.ComponentModel;

namespace VoxSign.Models;

/// <summary>Un repère illustré du dictionnaire LSF.</summary>
public partial class Sign : ObservableObject
{
    public required string Id { get; init; }

    public required string Word { get; init; }

    /// <summary>Catégorie affichée (« Politesse », « Au quotidien »…).</summary>
    public required string Category { get; init; }

    public required string CategoryKey { get; init; }

    /// <summary>Source d'image résolue : fichier téléchargé, image embarquée ou adresse web.</summary>
    public required string Image { get; init; }

    /// <summary>Titre de la fiche (« Pour dire merci »).</summary>
    public string Usage { get; init; } = string.Empty;

    public string UsageDescription { get; init; } = string.Empty;

    /// <summary>Exemple dans un échange (« Merci pour votre aide. »).</summary>
    public string Example { get; init; } = string.Empty;

    /// <summary>Disponible sans connexion (fait partie d'un pack présent sur l'appareil).</summary>
    public bool IsOffline { get; init; }

    [ObservableProperty]
    bool isFavorite;

    public string FavoriteCaption => $"LSF · {Category} · {(IsOffline ? "hors ligne" : "en ligne")}";
}
