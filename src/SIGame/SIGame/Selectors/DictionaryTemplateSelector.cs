using SIGame.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SIGame.Selectors;

public sealed class DictionaryTemplateSelector : DataTemplateSelector
{
    public Dictionary<string, DataTemplate> Views { get; set; } = new();

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not GameViewModel game)
        {
            return null;
        }

        if (Views.TryGetValue(game.Data.DialogMode.ToString(), out var result))
        {
            return result;
        }

        return null;
    }
}
