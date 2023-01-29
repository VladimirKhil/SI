using Notions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

/// <summary>
/// Trims value so it does not exceed specified length.
/// </summary>
public sealed class TrimConverter : IValueConverter
{
    /// <summary>
    /// Maximum value length.
    /// </summary>
    public int MaxLength { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((string)value).LeaveFirst(MaxLength);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
