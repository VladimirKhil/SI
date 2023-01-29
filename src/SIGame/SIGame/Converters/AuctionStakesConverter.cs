using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

/// <summary>
/// Преобразует числовые коды в ставку на Аукционе
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
public sealed class AuctionStakesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var stake = System.Convert.ToInt32(value);

        return stake switch
        {
            -1 => SICore.Properties.Resources.Nominal,
            -2 => string.Empty,// Pass
            -3 => SICore.Properties.Resources.VaBank,
            -4 => "######",
            _ => Notions.Notion.FormatNumber(stake),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
