using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

/// <summary>
/// Converts stakes numeric codes to localized strings.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
public sealed class AuctionStakesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "";
        }

        var stake = System.Convert.ToInt32(value);

        return stake switch
        {
            -1 => Resources.Nominal,
            -2 => "",// Pass
            -3 => Resources.VaBank,
            -4 => "######",
            _ => Notions.Notion.FormatNumber(stake),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
