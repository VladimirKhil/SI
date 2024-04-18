using SIPackages;

namespace SIEngine.Tests;

internal class SIEnginePlayHandlerMock : ISIEnginePlayHandler
{
    public Action<int, int>? SelectQuestion;

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        SelectQuestion = selectCallback;
    }

    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        
    }

    public void CancelQuestionSelection()
    {
        
    }

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)
    {
        
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        
    }

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        
    }

    public void OnThemeDeleted(int themeIndex)
    {
        
    }

    public void OnThemeSelected(int themeIndex)
    {
        
    }

    public bool ShouldPlayQuestionForAll()
    {
        return true;
    }
}
