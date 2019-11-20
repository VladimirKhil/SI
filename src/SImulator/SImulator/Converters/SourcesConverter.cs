using System;
using System.Text;
using System.Windows.Data;
using System.Collections;

namespace SImulator.Converters
{
    public sealed class SourcesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is IEnumerable sources))
                return null;

            var result = new StringBuilder();
            foreach (var item in sources)
            {
                if (result.Length > 0)
                    result.Append(", ");
                result.Append(item);
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
