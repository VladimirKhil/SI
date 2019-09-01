using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace SIGame.UI.Converters
{
    [Obsolete]
    [ValueConversion(typeof(string), typeof(double))]
    public sealed class TextToFontSizeConverter : IValueConverter
    {
        /// <summary>
        /// Базовый размер шрифта, который уменьшается при увеличении длины текста
        /// </summary>
        public double BaseFontSize { get; set; }
        /// <summary>
        /// Количество символов, соответствующих уменьшению шрифта на один пункт
        /// </summary>
        public int TextLengthForOnePoint { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return this.BaseFontSize;

            var text = value.ToString();
            return Math.Max(this.BaseFontSize / 3, this.BaseFontSize - (double)text.Length / this.TextLengthForOnePoint);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
