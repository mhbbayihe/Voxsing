namespace VoxSign.Shared;

/// <summary>
/// Catalogue initial : insère la base de l'API au premier démarrage, et sert de contenu embarqué
/// dans l'application pour qu'elle fonctionne hors ligne dès la première ouverture.
/// </summary>
public static class SeedCatalog
{
    public record SeedCategory(string Key, string Label, string ShortLabel);

    public record SeedSign(string Id, string Word, string CategoryKey, string ImageKey, string Usage, string UsageDescription, string Example, string[] Keywords);

    public record SeedTheme(string Key, string Label, string Title, string Description);

    public record SeedPhrase(string Id, string Text, string ThemeKey, string Intent, string? Context = null);

    public record SeedPack(string Id, string Name, string Icon, int Version, string[] SignIds, string[] PhraseIds);

    public static IReadOnlyList<SeedCategory> Categories { get; } =
    [
        new("politesse", "Politesse", "Politesse"),
        new("quotidien", "Au quotidien", "Quotidien"),
        new("sante", "Santé & besoins", "Santé"),
        new("transport", "Transport", "Transport"),
        new("emotions", "Émotions", "Émotions"),
    ];

    public static IReadOnlyList<SeedSign> Signs { get; } =
    [
        new("bonjour", "Bonjour", "politesse", "bonjour",
            "Pour saluer",
            "Saluer une personne en arrivant, au début d’un échange.",
            "« Bonjour, comment allez-vous ? »",
            ["bonjour", "salut", "bonsoir"]),
        new("merci", "Merci", "politesse", "merci",
            "Pour dire merci",
            "Exprimer sa gratitude après un service, une attention ou un échange.",
            "« Merci pour votre aide. »",
            ["merci", "remercie", "remercier"]),
        new("svp", "S’il vous plaît", "politesse", "svp",
            "Pour demander poliment",
            "Accompagner une demande, une commande ou une question.",
            "« Un verre d’eau, s’il vous plaît. »",
            ["s'il vous plaît", "s'il te plaît", "svp", "stp"]),
        new("cafe", "Café", "quotidien", "cafe",
            "Pour parler du café",
            "Commander une boisson ou proposer une pause.",
            "« On prend un café ? »",
            ["café", "expresso"]),
        new("aide", "Aide", "sante", "aide",
            "Pour demander de l’aide",
            "Signaler un besoin, une difficulté ou une urgence.",
            "« J’ai besoin d’aide. »",
            ["aide", "aider", "aidez", "secours"]),
    ];

    public static IReadOnlyList<SeedTheme> Themes { get; } =
    [
        new("cafe", "Au café", "Une pause au café", "Commander et se faire comprendre"),
        new("sante", "Santé", "Chez le médecin", "Expliquer ce que l’on ressent"),
        new("transport", "Transport", "En déplacement", "Demander son chemin, acheter un billet"),
        new("accueil", "Accueil", "Premiers échanges", "Se présenter et se comprendre"),
        new("travail", "Au travail", "Au travail", "S’organiser avec ses collègues"),
        new("famille", "En famille", "En famille", "Partager le quotidien avec ses proches"),
    ];

    public static IReadOnlyList<SeedPhrase> Phrases { get; } =
    [
        new("cafe-commander", "Bonjour, je voudrais un café, s’il vous plaît.", "cafe", "Commander"),
        new("cafe-emporter", "À emporter, s’il vous plaît.", "cafe", "Préciser"),
        new("cafe-prix", "Combien ça coûte ?", "cafe", "Payer"),
        new("cafe-merci", "Merci, bonne journée !", "cafe", "Saluer"),
        new("cafe-sur-place", "Sur place, s’il vous plaît.", "cafe", "Préciser"),
        new("cafe-sucre", "Un café sans sucre, merci.", "cafe", "Commander"),
        new("cafe-carte", "Je peux payer par carte ?", "cafe", "Payer"),
        new("cafe-addition", "L’addition, s’il vous plaît.", "cafe", "Payer"),
        new("sante-aide", "J’ai besoin d’aide.", "sante", "Demander"),
        new("sante-douleur", "J’ai mal ici.", "sante", "Expliquer"),
        new("sante-medecin", "Pouvez-vous appeler un médecin ?", "sante", "Demander"),
        new("transport-arret", "Où est l’arrêt de bus ?", "transport", "Demander"),
        new("transport-billet", "Un billet pour le centre-ville, s’il vous plaît.", "transport", "Acheter"),
        new("transport-horaire", "À quelle heure part le prochain train ?", "transport", "Demander"),
        new("accueil-sourde", "Je suis sourde. Écrivez-moi, s’il vous plaît.", "accueil", "Me présenter", "Me présenter"),
        new("accueil-repeter", "Pouvez-vous répéter plus lentement ?", "accueil", "Se comprendre", "Se comprendre"),
        new("accueil-prenom", "Bonjour, je m’appelle Daniela.", "accueil", "Me présenter", "Me présenter"),
        new("travail-ecrit", "Pouvez-vous m’envoyer le message par écrit ?", "travail", "S’organiser"),
        new("famille-dimanche", "Tu viens manger dimanche ?", "famille", "Inviter"),
    ];

    public static IReadOnlyList<SeedPack> Packs { get; } =
    [
        new("essentiels", "Les essentiels", "HandHeart", 1,
            ["bonjour", "merci", "svp", "aide"],
            ["accueil-sourde", "accueil-repeter", "accueil-prenom", "cafe-merci"]),
        new("quotidien", "Au quotidien", "Coffee", 1,
            ["cafe", "bonjour", "merci", "svp"],
            ["cafe-commander", "cafe-emporter", "cafe-prix", "cafe-merci", "cafe-sur-place", "cafe-sucre", "cafe-carte", "cafe-addition"]),
        new("sante", "Santé & besoins", "HeartPulse", 1,
            ["aide", "svp", "merci"],
            ["sante-aide", "sante-douleur", "sante-medecin"]),
    ];

    /// <summary>Packs présents sur l'appareil dès l'installation.</summary>
    public static IReadOnlyList<string> BundledPackIds { get; } = ["essentiels", "quotidien"];

    /// <summary>Compte de démonstration (créé uniquement en environnement de développement).</summary>
    public static class DemoAccount
    {
        public const string FirstName = "Daniela";
        public const string Email = "danielacledya@gmail.fr";
        public const string Password = "voxsign-2026";
        public static readonly string[] FavoriteSignIds = ["bonjour", "merci"];
        public static readonly string[] FavoritePhraseIds = ["cafe-emporter", "accueil-sourde", "accueil-repeter"];
    }

    public static SignDto ToDto(SeedSign sign, string imageBaseUrl) => new(
        sign.Id,
        sign.Word,
        sign.CategoryKey,
        Categories.First(c => c.Key == sign.CategoryKey).Label,
        sign.ImageKey,
        $"{imageBaseUrl.TrimEnd('/')}/{sign.ImageKey}.jpg",
        sign.Usage,
        sign.UsageDescription,
        sign.Example,
        sign.Keywords);

    public static PhraseDto ToDto(SeedPhrase phrase) => new(
        phrase.Id,
        phrase.Text,
        phrase.ThemeKey,
        Themes.First(t => t.Key == phrase.ThemeKey).Label,
        phrase.Intent,
        phrase.Context,
        IsCustom: false);
}
