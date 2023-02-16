using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SIEngine.Tests;

internal sealed class QuestionEnginePlayHandlerMock : IQuestionEnginePlayHandler
{
    public void OnAccept()
    {
        throw new System.NotImplementedException();
    }

    public void OnAskAnswer(string mode)
    {
        throw new System.NotImplementedException();
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        throw new System.NotImplementedException();
    }

    public void OnQuestionStart()
    {
        throw new System.NotImplementedException();
    }

    public void OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        throw new System.NotImplementedException();
    }

    public void OnSetPrice(string mode, NumberSet? availableRange)
    {
        throw new System.NotImplementedException();
    }

    public void OnSetTheme(string themeName)
    {
        throw new System.NotImplementedException();
    }

    public void OnSimpleRightAnswerStart()
    {
        throw new System.NotImplementedException();
    }
}
