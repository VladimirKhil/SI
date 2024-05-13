using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SIGame.Converters;

public class DictionaryJoinConverter<T> : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Join(", ", ((IDictionary<string, T>)value).Select(kvp => $"{kvp.Key}: {kvp.Value}"));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public sealed class DictionaryStringJoinConverter : DictionaryJoinConverter<string> { }

public sealed class DictionaryInt32JoinConverter : DictionaryJoinConverter<int> { }
