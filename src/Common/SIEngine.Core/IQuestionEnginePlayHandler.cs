using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Handles <see cref="QuestionEngine" /> play events.
/// </summary>
public interface IQuestionEnginePlayHandler
{
    /// <summary>
    /// Shows question content item.
    /// </summary>
    /// <param name="contentItem">Question content item.</param>
    void OnQuestionContentItem(ContentItem contentItem);

    /// <summary>
    /// Asks for the answer.
    /// </summary>
    /// <param name="mode">Ask answer mode.</param>
    void OnAskAnswer(string mode);

    /// <summary>
    /// Allows to press the button.
    /// </summary>
    void OnButtonPressStart();

    /// <summary>
    /// Sets question answerer(s).
    /// </summary>
    /// <param name="mode">Set answerer mode.</param>
    /// <param name="select">Selection rule.</param>
    /// <param name="stakeVisibility">Stake visibility for stakes mode.</param>
    void OnSetAnswerer(string mode, string? select, string? stakeVisibility);

    /// <summary>
    /// Sets price for the answerers. Positive and negative prices could be set separately.
    /// Prices could be different for each answerer.
    /// </summary>
    /// <param name="mode">Set price mode.</param>
    /// <param name="availableRange">Alavilable prices to select.</param>
    void OnSetPrice(string mode, NumberSet? availableRange);

    /// <summary>
    /// Sets theme name.
    /// </summary>
    /// <param name="themeName">Theme name to set.</param>
    void OnSetTheme(string themeName);

    /// <summary>
    /// Accepts the question as answered right even if no answer has been provided.
    /// </summary>
    void OnAccept();

    /// <summary>
    /// Handles question start.
    /// </summary>
    /// <param name="buttonsRequired">Whether the question requires buttons to play.</param>
    void OnQuestionStart(bool buttonsRequired);

    /// <summary>
    /// Handles content start.
    /// </summary>
    void OnContentStart();

    /// <summary>
    /// Handles simple right answer start.
    /// </summary>
    void OnSimpleRightAnswerStart();

    /// <summary>
    /// Handles the ending of asking for an answer.
    /// </summary>
    void OnAskAnswerStop();
}
