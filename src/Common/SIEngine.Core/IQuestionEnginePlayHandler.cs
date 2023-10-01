using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Handles <see cref="QuestionEngine" /> play events.
/// </summary>
public interface IQuestionEnginePlayHandler
{
    /// <summary>
    /// Sets answer options.
    /// </summary>
    /// <param name="answerOptions">Answer options.</param>
    bool OnAnswerOptions(AnswerOption[] answerOptions);

    /// <summary>
    /// Shows question content.
    /// </summary>
    /// <param name="content">Question content to display.</param>
    void OnQuestionContent(IReadOnlyCollection<ContentItem> content);

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
    bool OnSetAnswerer(string mode, string? select, string? stakeVisibility);

    /// <summary>
    /// Announces question price.
    /// </summary>
    /// <param name="availableRange">Alavilable prices to select.</param>
    bool OnAnnouncePrice(NumberSet? availableRange);

    /// <summary>
    /// Sets price for the answerers. Positive and negative prices could be set separately.
    /// Prices could be different for each answerer.
    /// </summary>
    /// <param name="mode">Set price mode.</param>
    /// <param name="availableRange">Alavilable prices to select.</param>
    bool OnSetPrice(string mode, NumberSet? availableRange);

    /// <summary>
    /// Sets theme name.
    /// </summary>
    /// <param name="themeName">Theme name to set.</param>
    bool OnSetTheme(string themeName);

    /// <summary>
    /// Accepts the question as answered right even if no answer has been provided.
    /// </summary>
    bool OnAccept();

    /// <summary>
    /// Handles question start.
    /// </summary>
    /// <param name="buttonsRequired">Whether the question requires buttons to play.</param>
    void OnQuestionStart(bool buttonsRequired);

    /// <summary>
    /// Handles content start.
    /// </summary>
    /// <param name="contentItems">Content items that would be played.</param>
    void OnContentStart(IEnumerable<ContentItem> contentItems);

    /// <summary>
    /// Handles simple right answer start.
    /// </summary>
    void OnSimpleRightAnswerStart();

    /// <summary>
    /// Handles right answer option for select answer type.
    /// </summary>
    /// <param name="rightOptionLabel">Right option label.</param>
    bool OnRightAnswerOption(string rightOptionLabel);

    /// <summary>
    /// Handles the ending of asking for an answer.
    /// </summary>
    void OnAskAnswerStop();
}
