using System.Windows.Controls;
using System.Windows;
using SIUI.ViewModel;

namespace SIUI.Selectors;

/// <summary>
/// Selects template for displaying question text.
/// </summary>
public sealed class QuestionTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Static text.
    /// </summary>
    public DataTemplate? Simple { get; set; }
    
    /// <summary>
    /// Text with animated reading ("karaoke" mode).
    /// </summary>
    public DataTemplate? Animated { get; set; }

    /// <summary>
    /// Text that appears partially (opacity animation).
    /// </summary>
    public DataTemplate? Partial { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is TableInfoViewModel info
            ? info.PartialText
                ? Partial
                : info.AnimateText
                    ? Animated
                    : Simple
            : base.SelectTemplate(item, container);
}
