using SIPackages.Core;
using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Selectors;

public sealed class QuestionTypeParamTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BaseTemplate { get; set; }

    public DataTemplate? CatThemeTemplate { get; set; }

    public DataTemplate? CatCostTemplate { get; set; }

    public DataTemplate? BagCatSelfTemplate { get; set; }

    public DataTemplate? BagCatKnowsTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        var param = ((QuestionTypeParamViewModel)item).Model;

        return param.Name switch
        {
            QuestionTypeParams.Cat_Theme => CatThemeTemplate,
            QuestionTypeParams.Cat_Cost => CatCostTemplate,
            QuestionTypeParams.BagCat_Self => BagCatSelfTemplate,
            QuestionTypeParams.BagCat_Knows => BagCatKnowsTemplate,
            _ => BaseTemplate
        };
    }
}
