using System.ComponentModel;
using System.Globalization;

namespace SIPackages.TypeConverters;

/// <summary>
/// Defines a type converter for <see cref="Info" /> class.
/// </summary>
/// <remarks>Responsible only for creating empty info for now.</remarks>
internal sealed class InfoTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string stringValue && stringValue.Length == 0 ? new Info() : base.ConvertFrom(context, culture, value);
}
