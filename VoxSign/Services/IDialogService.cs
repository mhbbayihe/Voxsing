namespace VoxSign.Services;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string accept = "OK");

    /// <summary>Feuille de choix ; renvoie l'option choisie ou <c>null</c>.</summary>
    Task<string?> ChooseAsync(string title, params string[] options);
}

public class ShellDialogService : IDialogService
{
    public Task AlertAsync(string title, string message, string accept = "OK") =>
        Shell.Current.DisplayAlert(title, message, accept);

    public async Task<string?> ChooseAsync(string title, params string[] options)
    {
        var choice = await Shell.Current.DisplayActionSheet(title, "Annuler", null, options);
        return options.Contains(choice) ? choice : null;
    }
}
