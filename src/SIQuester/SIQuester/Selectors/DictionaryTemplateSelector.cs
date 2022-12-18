using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Selectors;

public sealed class DictionaryTemplateSelector : DataTemplateSelector
{
    public Dictionary<Type, DataTemplate> Templates { get; set; } = new();

    public override DataTemplate SelectTemplate(object item, DependencyObject container) =>
        item != null && Templates.TryGetValue(item.GetType(), out var template)
            ? template
            : base.SelectTemplate(item, container);
}
