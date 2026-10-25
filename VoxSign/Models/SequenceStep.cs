using CommunityToolkit.Mvvm.ComponentModel;

namespace VoxSign.Models;

/// <summary>Une étape de la séquence illustrée d'une phrase.</summary>
public partial class SequenceStep(int number, Sign sign) : ObservableObject
{
    public int Number { get; } = number;

    public Sign Sign { get; } = sign;

    public string Caption => $"{Number} · {Sign.Word}";

    [ObservableProperty]
    bool isCurrent;
}
