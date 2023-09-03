using SIUI.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors;

/// <summary>
/// Allows to display collection of content in grid adjusting content height to it's weight.
/// </summary>
internal static class ContentCollectionBehavior
{
    public static TableInfoViewModel? GetIsAttached(DependencyObject obj) => (TableInfoViewModel?)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, TableInfoViewModel value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(TableInfoViewModel),
            typeof(ContentCollectionBehavior),
            new PropertyMetadata(null, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid)
        {
            return;
        }

        grid.RowDefinitions.Clear();

        if (e.NewValue is not TableInfoViewModel tableInfoViewModel)
        {
            return;
        }

        var content = tableInfoViewModel.Content;

        if (content == null)
        {
            return;
        }

        foreach (var contentItem in content)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(contentItem.Weight, GridUnitType.Star) });
        }
    }
}
