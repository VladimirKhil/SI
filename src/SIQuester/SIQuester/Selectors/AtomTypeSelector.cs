using SIPackages;
using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Selectors
{
    public sealed class AtomTypeSelector: DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate AudioTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }

        public AtomTypeSelector()
        {

        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var mediaItem = (MediaItemViewModel)item;

            switch (mediaItem.Type)
            {
                case SIDocument.ImagesStorageName:
                    return ImageTemplate;

                case SIDocument.AudioStorageName:
                    return AudioTemplate;

                case SIDocument.VideoStorageName:
                    return VideoTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
