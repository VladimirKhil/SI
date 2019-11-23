using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace SIQuester.Selectors
{
    public sealed class DictionaryTemplateSelector : DataTemplateSelector
    {
        public Dictionary<Type, DataTemplate> Templates { get; set; }

        public DictionaryTemplateSelector()
        {
            Templates = new Dictionary<Type, DataTemplate>();
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null)
            {
                if (Templates.TryGetValue(item.GetType(), out DataTemplate template))
                    return template;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
