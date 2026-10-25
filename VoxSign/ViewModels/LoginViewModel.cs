using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Services;

namespace VoxSign.ViewModels;

public partial class LoginViewModel(INavigationService navigation, IAuthService auth, IDialogService dialogs) : BaseViewModel(navigation)
{
    bool _sessionChecked;

    // Valeurs de démonstration reprises de la maquette (compte créé par l'API en développement)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    string email = "danielacledya@gmail.fr";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    string password = "voxsign-2026";

    [ObservableProperty]
    bool isPasswordHidden = true;

    /// <summary>Au lancement : une session enregistrée mène directement à l'accueil, même hors ligne.</summary>
    public override async Task LoadAsync()
    {
        if (_sessionChecked || App.CaptureDirectory is not null)
            return;

        _sessionChecked = true;
        if (await auth.RestoreSessionAsync())
            await Navigation.GoToAsync($"//{Routes.Home}");
    }

    // La commande asynchrone se désactive d'elle-même pendant la connexion
    bool CanSignIn() => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    async Task SignInAsync()
    {
        IsBusy = true;
        try
        {
            var result = await auth.SignInAsync(Email, Password);
            if (result.Succeeded)
                await Navigation.GoToAsync($"//{Routes.Home}");
            else
                await dialogs.AlertAsync("Connexion impossible", result.Error ?? "Vérifiez votre adresse e-mail et votre mot de passe.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    void TogglePasswordVisibility() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    Task ForgotPasswordAsync() =>
        dialogs.AlertAsync("Mot de passe oublié", "La réinitialisation par e-mail n’est pas encore disponible. Contactez le support VoxSign.");

    [RelayCommand]
    Task CreateAccountAsync() => Navigation.GoToAsync(Routes.Register);

    [RelayCommand]
    Task ExploreAsync()
    {
        auth.ContinueAsGuest();
        return Navigation.GoToAsync($"//{Routes.Home}");
    }
}
