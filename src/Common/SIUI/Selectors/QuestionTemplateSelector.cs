using System.Windows.Controls;
using System.Windows;
using SIUI.ViewModel;

namespace SIUI.Selectors
{
    public sealed class QuestionTemplateSelector: DataTemplateSelector
    {
        public DataTemplate Simple { get; set; }
        public DataTemplate Animated { get; set; }
		public DataTemplate Partial { get; set; }

		public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
			if (item is TableInfoViewModel info)
				return info.PartialText ? Partial : info.AnimateText ? Animated : Simple;

			return base.SelectTemplate(item, container);
        }
    }
}
