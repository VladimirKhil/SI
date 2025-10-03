using SICore;
using SIData;

namespace SImulator.ViewModel.Controllers;

/// <summary>
/// Provides new version of game controller.
/// </summary>
internal sealed class GameController : IPersonController
{
    public GameViewModel? GameViewModel { get; set; }

    public bool CanSwitchType => throw new NotImplementedException();

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        throw new NotImplementedException();
    }

    public void ClearSelections(bool full = false)
    {
        throw new NotImplementedException();
    }

    public void DeleteTheme()
    {
        throw new NotImplementedException();
    }

    public void EndThink()
    {
        throw new NotImplementedException();
    }

    public void EndTry(string text)
    {
        throw new NotImplementedException();
    }

    public void FinalThink()
    {
        throw new NotImplementedException();
    }

    public void IsRight(string name, bool voteForRight, string answer)
    {
        throw new NotImplementedException();
    }

    public void MakeStake()
    {
        throw new NotImplementedException();
    }

    public void OnPauseChanged(bool isPaused)
    {
        throw new NotImplementedException();
    }

    public void OnPersonApellated(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonFinalAnswer(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonFinalStake(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonPass(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerOutcome(int playerIndex, bool isRight)
    {
        throw new NotImplementedException();
    }

    public void OnReplic(string personCode, string text)
    {
        throw new NotImplementedException();
    }

    public void OnRightAnswer(string answer)
    {
        throw new NotImplementedException();
    }

    public void OnTextSpeed(double speed)
    {
        throw new NotImplementedException();
    }

    public void OnTheme(string themeName, int questionCount, bool animate)
    {
        throw new NotImplementedException();
    }

    public void OnTimeChanged()
    {
        throw new NotImplementedException();
    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null)
    {
        throw new NotImplementedException();
    }

    public void Out(int themeIndex)
    {
        throw new NotImplementedException();
    }

    public void PrintGreeting()
    {
        throw new NotImplementedException();
    }

    public void ReceiveText(Message m)
    {
        throw new NotImplementedException();
    }

    public void Report(string report)
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }

    public void SelectQuestion()
    {
        throw new NotImplementedException();
    }

    public void ShowTablo()
    {
        throw new NotImplementedException();
    }

    public void Stage()
    {
        throw new NotImplementedException();
    }

    public void StopRound()
    {
        throw new NotImplementedException();
    }

    public void TableLoaded()
    {
        throw new NotImplementedException();
    }

    public void TimeOut()
    {
        throw new NotImplementedException();
    }

    public void UpdatePicture(Account account, string path)
    {
        throw new NotImplementedException();
    }

    public void ValidateAnswer(int playerIndex, string answer)
    {
        throw new NotImplementedException();
    }
}
