using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class MappedConverter : IValueConverter
{
    private StringDictionary? _map = null;

    public object Map
    {
        get => _map;
        set
        {
            if (value is StringDictionary dict)
            {
                _map = dict;
                return;
            }

            if (value is CollectionViewSource collection)
            {
                _map = new StringDictionary();

                foreach (KeyValuePair<string, string> item in collection.View)
                {
                    _map[item.Key] = item.Value;
                }
            }
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "";
        }

        if (_map.TryGetValue(value.ToString(), out string result))
        {
            return result;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var val = value.ToString();

        foreach (var item in _map)
        {
            if (item.Value == val)
            {
                return item.Key;
            }
        }

        return value;
    }
}
