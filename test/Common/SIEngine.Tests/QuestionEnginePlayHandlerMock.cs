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

    public void OnAskAnswer(string mode)
    {
        throw new NotImplementedException();
    }

    public void OnAskAnswerStop()
    {
        throw new NotImplementedException();
    }

    public void OnButtonPressStart()
    {
        throw new NotImplementedException();
    }

    public void OnContentStart(IEnumerable<ContentItem> contentItems)
    {
        throw new NotImplementedException();
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        throw new NotImplementedException();
    }

    public void OnQuestionStart(bool buttonsRequired)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
