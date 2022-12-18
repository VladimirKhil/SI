using System.ComponentModel;
using System.Globalization;

namespace SIPackages.TypeConverters;

/// <summary>
/// Defines a type converter for <see cref="QuestionType" /> class.
/// </summary>
internal sealed class QuestionTypeTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string stringValue ? new QuestionType { Name = stringValue } : base.ConvertFrom(context, culture, value);
}
