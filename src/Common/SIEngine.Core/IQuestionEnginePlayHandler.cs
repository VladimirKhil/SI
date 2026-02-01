using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Handles <see cref="QuestionEngine" /> play events.
/// </summary>
public interface IQuestionEnginePlayHandler
{
    /// <summary>
    /// Handles question start.
    /// </summary>
    /// <param name="buttonsRequired">Whether the question requires buttons to play.</param>
    /// <param name="rightAnswers">Question right answers.</param>
    /// <param name="skipQuestionCallback">Callback that allows to skip the question.</param>
    void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback);

    /// <summary>
    /// Sets answer options.
    /// </summary>
    /// <param name="answerOptions">Answer options.</param>
    /// <param name="screenContentSequence">Screen content sequence.</param>
    bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence);

    /// <summary>
    /// Notifies about numeric answer type.
    /// </summary>
    /// <param name="deviation">Acceptable numeric answer deviation.</param>
    bool OnNumericAnswerType(int deviation);

    /// <summary>
    /// Notifies about point answer type.
    /// </summary>
    /// <param name="deviation">Acceptable point answer deviation.</param>
    bool OnPointAnswerType(double deviation);

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
    bool OnButtonPressStart();

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
    bool OnAnnouncePrice(NumberSet availableRange);

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
    /// Handles content start.
    /// </summary>
    /// <param name="contentItems">Content items that would be played.</param>
    /// <param name="moveToContentCallback">Callback that allows to move play to specific content by index.</param>
    void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback);

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
    /// Handles right point answer.
    /// </summary>
    /// <param name="rightAnswer">Right point coordinates.</param>
    bool OnRightAnswerPoint(string rightAnswer);

    /// <summary>
    /// Handles answer start.
    /// </summary>
    void OnAnswerStart();
}
