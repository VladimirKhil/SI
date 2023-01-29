using System.Windows.Controls;
using System.Windows;
using SIUI.ViewModel;

namespace SIUI.Selectors;

public sealed class QuestionTemplateSelector : DataTemplateSelector
{
    public DataTemplate Simple { get; set; }

    public DataTemplate Animated { get; set; }

    public DataTemplate Partial { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) =>
        item is TableInfoViewModel info
            ? info.PartialText ? Partial : info.AnimateText ? Animated : Simple
            : base.SelectTemplate(item, container);
}
