using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Services;

namespace VoxSign.ViewModels;

public partial class RegisterViewModel(INavigationService navigation, IAuthService auth, IDialogService dialogs) : BaseViewModel(navigation)
{
    const int MinimumPasswordLength = 8;

    static readonly string[] SignLanguages =
    [
        "LSF · Langue des signes française",
        "LSFB · Langue des signes de Belgique francophone",
        "LSQ · Langue des signes québécoise",
        "LSF-SR · Langue des signes suisse romande",
    ];

    // Valeurs de démonstration reprises de la maquette
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    string firstName = "Daniela";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    string email = "danielacledya@gmail.fr";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordHint), nameof(IsPasswordLongEnough))]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    string password = "voxsign-2026";

    [ObservableProperty]
    bool isPasswordHidden = true;

    [ObservableProperty]
    string signLanguage = SignLanguages[0];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    bool acceptsTerms = true;

    public bool IsPasswordLongEnough => Password.Length >= MinimumPasswordLength;

    public string PasswordHint => IsPasswordLongEnough
        ? $"{Password.Length} caractères · mot de passe suffisamment long"
        : $"{Password.Length} caractères · {MinimumPasswordLength} minimum";

    bool CanCreateAccount() =>
        AcceptsTerms && IsPasswordLongEnough && !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(Email);

    [RelayCommand(CanExecute = nameof(CanCreateAccount))]
    async Task CreateAccountAsync()
    {
        IsBusy = true;
        try
        {
            var result = await auth.RegisterAsync(FirstName, Email, Password, SignLanguage.Split(' ')[0]);
            if (result.Succeeded)
                await Navigation.GoToAsync($"//{Routes.Home}");
            else
                await dialogs.AlertAsync("Inscription impossible", result.Error ?? "Vérifiez les informations saisies.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    void TogglePasswordVisibility() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    void ToggleTerms() => AcceptsTerms = !AcceptsTerms;

    [RelayCommand]
    async Task ChooseSignLanguageAsync()
    {
        if (await dialogs.ChooseAsync("Votre langue des signes", SignLanguages) is { } choice)
            SignLanguage = choice;
    }

    [RelayCommand]
    Task GoToLoginAsync() => Navigation.GoBackAsync();
}
