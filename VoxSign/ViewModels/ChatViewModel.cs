using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Models;
using VoxSign.Services;

namespace VoxSign.ViewModels;

/// <summary>
/// Échanges avec un proche : conversation d'exemple (la messagerie n'a pas encore de service distant),
/// dont les repères viennent du dictionnaire.
/// </summary>
public partial class ChatViewModel(INavigationService navigation, IContentService content, IDialogService dialogs, IShare share) : BaseViewModel(navigation)
{
    public string Contact => "Alex";

    public string Title => $"Avec {Contact}";

    public string Status => "Contact ajouté · en ligne";

    public string ContactsLabel => "Mes proches · 3";

    public string DayLabel => "Aujourd’hui · 9:38";

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    string draft = string.Empty;

    public override async Task LoadAsync()
    {
        if (Messages.Count > 0)
            return;

        const string order = "Bonjour, je voudrais un café, s’il vous plaît.";
        var merci = await content.GetSignAsync("merci");
        Messages.Add(new ChatMessage { Kind = ChatMessageKind.Received, Text = "Je t’attends au café. Tu commandes quoi ?", Meta = $"{Contact} · 9:38" });
        Messages.Add(new ChatMessage { Kind = ChatMessageKind.IllustratedSent, Text = order, Meta = $"Vous · 9:39 · Lu par {Contact}", Signs = await content.TranslateAsync(order) });
        Messages.Add(new ChatMessage { Kind = ChatMessageKind.Received, Text = "D’accord, je te le prends à emporter !", Meta = $"{Contact} · 9:40" });
        if (merci is not null)
            Messages.Add(new ChatMessage { Kind = ChatMessageKind.SignSent, Text = "Merci !", Meta = "Vous · 9:41 · Illustration LSF", Signs = [merci] });
    }

    bool CanSend() => !string.IsNullOrWhiteSpace(Draft);

    [RelayCommand(CanExecute = nameof(CanSend))]
    async Task SendAsync()
    {
        var text = Draft.Trim();
        Draft = string.Empty;
        var signs = await content.TranslateAsync(text);
        Messages.Add(new ChatMessage
        {
            Kind = signs.Count > 0 ? ChatMessageKind.IllustratedSent : ChatMessageKind.Received,
            Text = text,
            Meta = $"Vous · {DateTime.Now:H:mm}",
            Signs = signs,
        });
    }

    [RelayCommand]
    Task SharePhraseAsync() => Navigation.GoToAsync(Routes.Phrases);

    [RelayCommand]
    Task InviteAsync() => share.RequestAsync(new ShareTextRequest
    {
        Title = "Inviter un proche",
        Text = "Rejoignez-moi sur VoxSign pour échanger en texte, en voix et en signes illustrés.",
    });

    [RelayCommand]
    Task ShowContactsAsync() => dialogs.ChooseAsync("Mes proches", Contact, "Camille", "Moussa");

    [RelayCommand]
    Task ShowProfileAsync() => dialogs.AlertAsync(Contact, Status);
}
