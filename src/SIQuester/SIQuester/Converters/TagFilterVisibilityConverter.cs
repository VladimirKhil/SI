using SIQuester.ViewModel;
using SIQuester.ViewModel.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Hides catalog tags and tag groups that do not match the text typed in the new tag box.
/// </summary>
/// <remarks>
/// Values: [0] a tag group or a tag, [1] the typed text, [2] (for a tag) its group.
/// </remarks>
public sealed class TagFilterVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var filter = SelectTagsViewModel.GetFilter(values.Length > 1 ? values[1] as string : null);

        var isVisible = values[0] switch
        {
            TagsRepository.TagGroup group => SelectTagsViewModel.IsGroupVisible(group, filter),
            TagsRepository.Tag tag when values.Length > 2 && values[2] is TagsRepository.TagGroup group =>
                SelectTagsViewModel.IsTagVisible(group, tag, filter),
            _ => true
        };

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
