using SIQuester.ViewModel;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class ContentCommandsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var contentItems = (IContentCollection)values[0];
        var files = (ICollection<MediaItemViewModel>)values[1];
        var contentType = (string)values[2];

        var commands = new List<UICommand>(files.Count + 2)
        {
            new(contentItems.AddFile, ViewModel.Properties.Resources.File, contentType),
            new(contentItems.LinkUri, ViewModel.Properties.Resources.Link, contentType)
        };

        foreach (var file in files)
        {
            commands.Add(new UICommand(contentItems.LinkFile, file.Name, (file, contentType)));
        }

        return commands;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
