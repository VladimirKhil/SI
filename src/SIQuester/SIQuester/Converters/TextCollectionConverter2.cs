using SIQuester.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class TextCollectionConverter2 : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var model = (LinksViewModel)value;
        var owner = model.Owner.Owner;

        while (owner.Owner != null)
        {
            owner = owner.Owner;
        }

        var doc = ((PackageViewModel)owner).Document;

        if (doc == null)
        {
            return DependencyProperty.UnsetValue;
        }

        return model is AuthorsViewModel ? doc.Authors.Collection : doc.Sources.Collection;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
