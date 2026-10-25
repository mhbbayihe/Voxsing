using VoxSign.Models;

namespace VoxSign.Controls;

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Received { get; set; }

    public DataTemplate? IllustratedSent { get; set; }

    public DataTemplate? SignSent { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        ((ChatMessage)item).Kind switch
        {
            ChatMessageKind.IllustratedSent => IllustratedSent!,
            ChatMessageKind.SignSent => SignSent!,
            _ => Received!,
        };
}
