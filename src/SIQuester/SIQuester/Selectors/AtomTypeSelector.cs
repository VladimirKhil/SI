using SIPackages;
using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Selectors;

public sealed class AtomTypeSelector : DataTemplateSelector
{
    public DataTemplate ImageTemplate { get; set; }

    public DataTemplate AudioTemplate { get; set; }

    public DataTemplate VideoTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var mediaItem = (MediaItemViewModel)item;

        return mediaItem.Type switch
        {
            SIDocument.ImagesStorageName => ImageTemplate,
            SIDocument.AudioStorageName => AudioTemplate,
            SIDocument.VideoStorageName => VideoTemplate,
            _ => base.SelectTemplate(item, container),
        };
    }
}
