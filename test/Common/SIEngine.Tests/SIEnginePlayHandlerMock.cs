using SIPackages;

namespace SIEngine.Tests;

internal class SIEnginePlayHandlerMock : ISIEnginePlayHandler
{
    public Action<int, int>? SelectQuestion;

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        SelectQuestion = selectCallback;
    }

    public void CancelQuestionSelection()
    {
        
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        
    }

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        
    }

    public bool ShouldPlayQuestionForAll()
    {
        return true;
    }
}
