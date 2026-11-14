using SIPackages.Core;
using SIQuester.Properties;
using SIQuester.ViewModel;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Gives a short caption for an action panel row: what the buttons of the row act on.
/// </summary>
/// <remarks>
/// The parameter is "item" for the second row (the active item inside a node).
/// </remarks>
public sealed class ToolbarCaptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isItemRow = Equals(parameter, "item");

        return value switch
        {
            PackageViewModel => isItemRow ? Resources.ToolbarLogo : Resources.Package,
            RoundViewModel => Resources.Round,
            ThemeViewModel => Resources.Theme,
            QuestionViewModel => Resources.Question,
            AnswersViewModel answers => answers.IsRight ? Resources.ToolbarRightAnswers : Resources.ToolbarWrongAnswers,
            ContentItemsViewModel content => IsAnswer(content) ? Resources.Answer : Resources.ToolbarQuestionBody,
            TagsViewModel => Resources.Tags,
            AuthorsViewModel => Resources.Authors,
            SourcesViewModel => Resources.Sources,
            _ => ""
        };
    }

    private static bool IsAnswer(ContentItemsViewModel content) =>
        content.Owner.Parameters?.Any(parameter => parameter.Key == QuestionParameterNames.Answer && parameter.Value.ContentValue == content) == true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
