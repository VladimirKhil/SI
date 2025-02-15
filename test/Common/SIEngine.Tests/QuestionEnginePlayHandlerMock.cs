using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SIEngine.Tests;

internal sealed class QuestionEnginePlayHandlerMock : IQuestionEnginePlayHandler
{
    public bool OnAccept()
    {
        throw new NotImplementedException();
    }

    public bool OnAnnouncePrice(NumberSet? availableRange)
    {
        throw new NotImplementedException();
    }

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

    public void OnQuestionStart(bool buttonsRequired, Action skipQuestionCallback)
    {
        
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        throw new NotImplementedException();
    }

    public bool OnSetPrice(string mode, NumberSet? availableRange)
    {
        throw new NotImplementedException();
    }

    public bool OnSetTheme(string themeName)
    {
        throw new NotImplementedException();
    }

    public void OnSimpleRightAnswerStart()
    {

    }

    public bool OnRightAnswerOption(string rightOptionLabel) => false;
}
