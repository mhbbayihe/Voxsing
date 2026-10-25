using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Controls;
using VoxSign.Services;
using VoxSign.Shared;

namespace VoxSign.ViewModels;

public partial class HomeViewModel(
    INavigationService navigation, IAuthService auth, IOfflinePackService packs, IDialogService dialogs) : BaseViewModel(navigation)
{
    static readonly (string Label, float Pitch)[] Voices = [("Voix normale", 1f), ("Voix plus grave", 0.8f), ("Voix plus aiguë", 1.25f)];

    // Deux espaces avant le prénom, comme dans la maquette
    public string Greeting => auth.CurrentUser is { } user ? $"Bonjour  {user.FirstName}" : "Bonjour";

    [ObservableProperty]
    string offlineSummary = "Gérer les packs";

    public override async Task LoadAsync()
    {
        OnPropertyChanged(nameof(Greeting));
        var count = (await packs.GetPacksAsync()).Count(p => p.IsDownloaded);
        OfflineSummary = $"{count} pack{(count > 1 ? "s" : "")} téléchargé{(count > 1 ? "s" : "")} · Gérer les packs";
    }

    [RelayCommand]
    Task OpenTextToSignAsync() => Navigation.GoToAsync(Routes.TextToSign);

    [RelayCommand]
    Task OpenVoiceToTextAsync() => Navigation.GoToAsync(Routes.VoiceToText);

    [RelayCommand]
    Task OpenPhrasesAsync() => Navigation.GoToAsync(Routes.Phrases);

    [RelayCommand]
    Task OpenExchangesAsync() => Navigation.GoToTabAsync(NavTab.Echanges);

    [RelayCommand]
    Task OpenOfflineAsync() => Navigation.GoToAsync(Routes.Offline);

    [RelayCommand]
    async Task OpenProfileAsync()
    {
        if (auth.CurrentUser is not { } user)
        {
            await Navigation.GoToAsync($"//{Routes.Login}");
            return;
        }

        switch (await dialogs.ChooseAsync($"{user.FirstName} · {user.Email}", "Synthèse vocale", "Se déconnecter"))
        {
            case "Synthèse vocale":
                if (await dialogs.ChooseAsync("Synthèse vocale", Voices.Select(v => v.Label).ToArray()) is { } label)
                {
                    var pitch = Voices.First(v => v.Label == label).Pitch;
                    var result = await auth.UpdateSpeechPreferencesAsync(user.Speech with { Pitch = pitch });
                    if (!result.Succeeded)
                        await dialogs.AlertAsync("Synthèse vocale", result.Error!);
                }
                break;
            case "Se déconnecter":
                await auth.SignOutAsync();
                await Navigation.GoToAsync($"//{Routes.Login}");
                break;
        }
    }
}
