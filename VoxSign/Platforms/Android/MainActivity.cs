using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace VoxSign;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Plein écran : le dégradé passe sous les barres système, comme dans la maquette
        // (les écrans réservent eux-mêmes 60 en haut et 34 sous la barre d'onglets).
        if (Window is { } window)
        {
            WindowCompat.SetDecorFitsSystemWindows(window, false);
            window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            if (WindowCompat.GetInsetsController(window, window.DecorView) is { } controller)
            {
                controller.AppearanceLightStatusBars = true;
                controller.AppearanceLightNavigationBars = true;
            }
        }
    }
}
