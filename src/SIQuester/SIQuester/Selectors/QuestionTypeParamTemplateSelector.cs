using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel;

namespace SIQuester.Selectors
{
    public sealed class QuestionTypeParamTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BaseTemplate { get; set; }
        public DataTemplate CatThemeTemplate { get; set; }
        public DataTemplate CatCostTemplate { get; set; }
        public DataTemplate BagCatSelfTemplate { get; set; }
        public DataTemplate BagCatKnowsTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var param = ((QuestionTypeParamViewModel)item).Model;

            if (param.Name == QuestionTypeParams.Cat_Theme)
                return CatThemeTemplate;

            if (param.Name == QuestionTypeParams.Cat_Cost)
                return CatCostTemplate;

            if (param.Name == QuestionTypeParams.BagCat_Self)
                return BagCatSelfTemplate;

            if (param.Name == QuestionTypeParams.BagCat_Knows)
                return BagCatKnowsTemplate;

            return BaseTemplate;
        }
    }
}
