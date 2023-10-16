using SIStorage.Service.Contract.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PlayedPackageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Package package
            && UserSettings.Default.PackageHistory.Contains(package.Id.ToString())
            ? FontWeights.Bold
            : FontWeights.Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
