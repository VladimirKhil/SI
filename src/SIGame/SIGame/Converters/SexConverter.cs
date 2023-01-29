using SICore;
using SIGame.Properties;
using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows.Data;

namespace SIGame.Converters;

[ValueConversion(typeof(PersonAccount), typeof(string))]
public sealed class SexConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var resourceManager = new ResourceManager("SIGame.Properties.Resources", typeof(Resources).Assembly);
        var parameterValue = resourceManager.GetString(parameter.ToString());

        if (Thread.CurrentThread.CurrentUICulture.Name != "ru-RU")
        {
            return parameterValue; // Form does not depend on sex
        }

        // ru-RU
        var isMale = (bool)value;
        var suffix = isMale ? "" : "а";

        return $"{parameterValue}{suffix}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
