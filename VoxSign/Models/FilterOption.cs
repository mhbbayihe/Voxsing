using CommunityToolkit.Mvvm.ComponentModel;

namespace VoxSign.Models;

/// <summary>Une option d'un filtre segmenté (« Tous », « Politesse »…).</summary>
public partial class FilterOption(string label, string key) : ObservableObject
{
    public string Label { get; } = label;

    public string Key { get; } = key;

    [ObservableProperty]
    bool isSelected;
}
