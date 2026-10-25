#if DEBUG
using VoxSign.Services;

namespace VoxSign.Diagnostics;

/// <summary>
/// Outil de recette visuelle : ouvre chaque écran à la hauteur de la maquette, l'enregistre en PNG
/// (pNN.png, même numérotation que l'export PDF) puis quitte. Activé par la variable VOXSIGN_CAPTURE.
/// </summary>
public static class DesignCapture
{
    // Hauteur de chaque écran dans l'export (sert au réassemblage contenu + barre d'onglets)
    static readonly (string File, string Route, double Height)[] Screens =
    [
        ("p01", $"//{Routes.Login}", 966),
        ("p02", $"//{Routes.Login}/{Routes.Register}", 972),
        ("p03", $"//{Routes.Home}", 998),
        ("p04", $"//{Routes.Home}/{Routes.TextToSign}", 967),
        ("p05", $"//{Routes.Home}/{Routes.VoiceToText}", 1029),
        ("p06", $"//{Routes.Home}/{Routes.Sequence}", 1005),
        ("p07", $"//{Routes.Dictionary}", 988),
        ("p08", $"//{Routes.Dictionary}/{Routes.SignDetail}?id=merci", 1058),
        ("p09", $"//{Routes.Home}/{Routes.Phrases}", 1061),
        ("p10", $"//{Routes.Favorites}", 1013),
        ("p11", $"//{Routes.Home}/{Routes.Offline}", 1025),
        ("p12", $"//{Routes.Chat}", 1029),
    ];

    public static async Task RunAsync(Window window, string directory)
    {
        Directory.CreateDirectory(directory);
        await Task.Delay(1500);

        // Largeur utile exactement égale à celle de la maquette (420)
        for (var i = 0; i < 3 && Shell.Current.CurrentPage is { Width: > 0 } first && Math.Abs(first.Width - 420) > 0.1; i++)
        {
            window.Width += 420 - first.Width;
            await Task.Delay(400);
        }

        // Recette du mode hors ligne : un favori marqué ici sans serveur doit être envoyé à la connexion suivante
        if (Environment.GetEnvironmentVariable("VOXSIGN_SELFTEST_FAVORITE") is { Length: > 0 } signId)
            await IPlatformApplication.Current!.Services.GetRequiredService<IFavoritesService>()
                .SetAsync(Services.Local.FavoriteRecord.SignKind, signId, true);

        // Recette des packs : « sante » télécharge le pack, « -sante » le retire
        if (Environment.GetEnvironmentVariable("VOXSIGN_SELFTEST_PACK") is { Length: > 0 } packId)
        {
            var offline = IPlatformApplication.Current!.Services.GetRequiredService<IOfflinePackService>();
            var pack = (await offline.GetPacksAsync(refresh: true)).First(p => p.Id == packId.TrimStart('-'));
            await (packId.StartsWith('-') ? offline.RemoveAsync(pack) : offline.DownloadAsync(pack));
        }

        var auth = IPlatformApplication.Current!.Services.GetRequiredService<IAuthService>();
        await auth.SignInAsync("danielacledya@gmail.fr", "voxsign-2026");

        var only = Environment.GetEnvironmentVariable("VOXSIGN_CAPTURE_ONLY");
        foreach (var (file, route, height) in Screens)
        {
            if (only is not null && !only.Split(',').Contains(file))
                continue;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route, false));
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(Path.Combine(directory, "errors.txt"), $"{file}: {ex}\n\n");
                continue;
            }

            await Task.Delay(1200);

            // La fenêtre ne peut pas dépasser la hauteur de l'écran : on capture le contenu défilant
            // en entier et la barre d'onglets à part (réassemblés ensuite sur le dégradé).
            var root = Shell.Current.CurrentPage switch
            {
                ContentPage { Content: var content } => content,
                _ => null,
            };
            var scroll = root as ScrollView ?? (root as Layout)?.OfType<ScrollView>().FirstOrDefault();
            if (scroll?.Content is VisualElement body)
            {
                // La capture perd la transparence : on peint le dégradé de la page derrière le contenu
                body.Background = Shell.Current.CurrentPage.Background;
                await Task.Delay(300);
                await SaveAsync(body, Path.Combine(directory, $"{file}_content.png"));
            }
            if ((root as Layout)?.OfType<Controls.BottomNav>().FirstOrDefault() is { } nav)
                await SaveAsync(nav, Path.Combine(directory, $"{file}_nav.png"));
        }

        Application.Current!.Quit();
    }

    static async Task SaveAsync(VisualElement element, string path)
    {
        var shot = await element.CaptureAsync();
        if (shot is null)
            return;

        await using var source = await shot.OpenReadAsync(ScreenshotFormat.Png);
        await using var target = File.Create(path);
        await source.CopyToAsync(target);
    }
}
#endif
