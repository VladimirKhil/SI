using System;
using System.Windows.Data;

namespace SIGame.Converters
{
    /// <summary>
    /// Преобразует числовые коды в ставку на Аукционе
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public sealed class AuctionStakesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var stake = System.Convert.ToInt32(value);
            switch (stake)
            {
                case -1:
                    return SICore.Properties.Resources.Nominal;

                case -2:
                    return string.Empty; // Пас

                case -3:
                    return SICore.Properties.Resources.VaBank;

                case -4:
                    return "######";

                default:
                    return Notions.Notion.FormatNumber(stake);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
