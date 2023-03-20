using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SICore.Clients.Game;

internal sealed class QuestionPlayHandler : IQuestionEnginePlayHandler
{
    public void OnAccept()
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

    public void OnQuestionStart()
    {
        throw new NotImplementedException();
    }

    public void OnQuestionStart(bool questionRequiresButtons)
    {
        throw new NotImplementedException();
    }

    public void OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        throw new NotImplementedException();
    }

    public void OnSetPrice(string mode, NumberSet? availableRange)
    {
        throw new NotImplementedException();
    }

    public void OnSetTheme(string themeName)
    {
        throw new NotImplementedException();
    }

    public void OnSimpleRightAnswerStart()
    {
        throw new NotImplementedException();
    }
}
