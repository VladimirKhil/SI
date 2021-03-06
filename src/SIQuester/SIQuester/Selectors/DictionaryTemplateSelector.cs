using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Selectors
{
    public sealed class DictionaryTemplateSelector : DataTemplateSelector
    {
        public Dictionary<Type, DataTemplate> Templates { get; set; }

        public DictionaryTemplateSelector()
        {
            Templates = new Dictionary<Type, DataTemplate>();
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) =>
            item != null && Templates.TryGetValue(item.GetType(), out var template)
                ? template
                : base.SelectTemplate(item, container);
    }
}
