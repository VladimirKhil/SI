using SIStorageService.Client.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PlayedPackageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is PackageInfo package && !string.IsNullOrEmpty(package.Guid)
            && UserSettings.Default.PackageHistory.Contains(package.Guid)
            ? FontWeights.Bold : FontWeights.Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
