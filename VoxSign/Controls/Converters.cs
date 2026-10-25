using System.Globalization;

namespace VoxSign.Controls;

public static class Converters
{
    public static IValueConverter IsNotNull { get; } = new FuncConverter(v => v is not null);

    public static IValueConverter Invert { get; } = new FuncConverter(v => v is not true);

    sealed class FuncConverter(Func<object?, object?> convert) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => convert(value);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
