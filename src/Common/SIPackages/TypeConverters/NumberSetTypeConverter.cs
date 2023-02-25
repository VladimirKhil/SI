using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using SIPackages.Core;

namespace SIPackages.TypeConverters;

/// <summary>
/// Provides helper method for parsing <see cref="NumberSet" /> type.
/// </summary>
/// <inheritdoc />
public sealed class NumberSetTypeConverter : TypeConverter
{
    private static readonly Regex NumberSetRegex = new(@"\[(?'min'\d+);(?'max'\d+)\](/(?'step'\d+))?", RegexOptions.Compiled);

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string stringValue ? ParseNumberSet(stringValue) : base.ConvertFrom(context, culture, value);

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) && base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string) && value is NumberSet numberSet
            ? numberSet.ToString()
            : base.ConvertTo(context, culture, value, destinationType);

    /// <summary>
    /// Parses number set from serialized syntax.
    /// Number set syntax examples are "[100;500]" or "[200;1000]/200".
    /// </summary>
    /// <param name="value">Number set value.</param>
    /// <returns>Parsed number set.</returns>
    public static NumberSet? ParseNumberSet(string value)
    {
        if (int.TryParse(value, out var singleNumber))
        {
            return new NumberSet
            {
                Minimum = singleNumber,
                Maximum = singleNumber,
                Step = 0
            };
        }

        var match = NumberSetRegex.Match(value);

        if (!match.Success)
        {
            return null;
        }

        _ = int.TryParse(match.Groups["min"].Value, out var minimum);
        _ = int.TryParse(match.Groups["max"].Value, out var maximum);
        var stepString = match.Groups["step"].Value;

        return new NumberSet
        {
            Minimum = minimum,
            Maximum = maximum,
            Step = GetStepValue(minimum, maximum, stepString)
        };
    }

    private static int GetStepValue(int minimum, int maximum, string stepString) =>
        stepString.Length > 0 && int.TryParse(stepString, out var step) ? step : maximum - minimum;
}
