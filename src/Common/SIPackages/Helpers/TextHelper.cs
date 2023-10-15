namespace SIPackages.Helpers;

internal static class TextHelper
{
    public static string LimitLengthBy(this string value, int? maxLength) =>
        (maxLength.HasValue && value.Length > maxLength.Value) ? string.Concat(value.AsSpan(0, maxLength.Value - 1), "…") : value;
}
