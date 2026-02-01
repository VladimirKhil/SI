using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SIEngine.Tests;

internal sealed class QuestionEnginePlayHandlerMock : IQuestionEnginePlayHandler
{
    public bool OnAccept() => false;

    public bool OnAnnouncePrice(NumberSet availableRange) => false;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence) => false;

    public void OnAskAnswer(string mode)
    {
        
    }

    public void OnAnswerStart()
    {
        
    }

    public bool OnButtonPressStart() => false;

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
    {
        
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        
    }

    public void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback)
    {
        
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility) => false;

    public bool OnSetPrice(string mode, NumberSet? availableRange) => false;

    public bool OnSetTheme(string themeName) => false;

    public void OnSimpleRightAnswerStart()
    {

    }

    public bool OnRightAnswerOption(string rightOptionLabel) => false;

    public bool OnNumericAnswerType(int deviation) => false;

    public bool OnPointAnswerType(double deviation) => false;

    public bool OnRightAnswerPoint(string rightAnswer) => false;
}
