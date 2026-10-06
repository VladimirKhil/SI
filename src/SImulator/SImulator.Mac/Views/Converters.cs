using Avalonia.Data.Converters;
using SImulator.ViewModel.Core;
using System.Globalization;

namespace SImulator.Views;

/// <summary>
/// Value converters used in SImulator views.
/// </summary>
internal static class Converters
{
    public static readonly IValueConverter IsStartMode = new FuncValueConverter<GameMode, bool>(mode => mode == GameMode.Start);

    public static readonly IValueConverter IsModeratorMode = new FuncValueConverter<GameMode, bool>(mode => mode == GameMode.Moderator);

    /// <summary>
    /// Compares the value with the converter parameter (by string representation, for enums).
    /// ConvertBack returns the parameter when the value is true (for radio buttons).
    /// </summary>
    public static readonly IValueConverter Equality = new EqualityConverter();

    public static readonly IValueConverter NotEmpty = new FuncValueConverter<object?, bool>(value => value switch
    {
        null => false,
        string text => text.Length > 0,
        _ => true,
    });

    public static readonly IValueConverter JoinLines = new FuncValueConverter<IEnumerable<string>?, string>(values => values != null ? string.Join(Environment.NewLine, values) : "");

    private sealed class EqualityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value?.ToString() == parameter?.ToString();

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true && parameter != null ? Enum.Parse(targetType, parameter.ToString()!) : Avalonia.Data.BindingOperations.DoNothing;
    }
}
