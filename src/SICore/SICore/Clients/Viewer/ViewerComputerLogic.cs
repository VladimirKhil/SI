using SICore.Contracts;
using SICore.Models;
using SIData;
using SIPackages;

namespace SICore;

/// <summary>
/// Defines a computer viewer logic.
/// </summary>
internal class ViewerComputerLogic : IPersonController
{
    protected readonly ViewerActions _viewerActions;

    public bool CanSwitchType => false;

    private readonly PlayerComputerController _player;
    private readonly ShowmanComputerController _showman;

    private readonly TimerInfo[] _timersInfo = new TimerInfo[] { new(), new(), new() };

    private readonly GameRole _role;

    private readonly ViewerData _data;

    internal ViewerComputerLogic(
        ViewerData data,
        ViewerActions viewerActions,
        IIntelligence intelligence,
        GameRole role)
    {
        _data = data;
        _viewerActions = viewerActions;
        _role = role;

        _player = new PlayerComputerController(data,  intelligence, viewerActions, _timersInfo);
        _showman = new ShowmanComputerController(data, viewerActions, intelligence);
    }

    public void ClearSelections(bool full = false)
    {
        if (_role == GameRole.Showman)
        {
            // TODO
        }
        else
        {
            _player.CancelTask();
        }
    }

    public void IsRight(string name, bool voteForRight, string answer)
    {
        if (_role == GameRole.Showman)
        {
            _showman.IsRight(answer);
        }
        else
        {
            _player.ValidateAnswer(voteForRight);
        }
    }

    public void OnSelectPlayer(SelectPlayerReason reason)
    {
        if (_role == GameRole.Showman)
        {
            _showman.SelectPlayer();
        }
        else
        {
            _player.SelectPlayer();
        }
    }

    public void OnTheme(string themeName, int questionCount, bool animate = false)
    {
        if (_role == GameRole.Player)
        {
            _player.OnTheme(questionCount);
        }
    }

    public void OnThemeInfo(string themeName)
    {
        if (_role == GameRole.Player)
        {
            _player.OnTheme(-1);
        }
    }

    public void StartThink() => _player.StartThink();

    public void EndThink() => _player.EndThink();

    public void Answer() => _player.Answer();

    public void OnPlayerOutcome(int playerIndex, bool isRight) => _player.PersonAnswered(playerIndex, isRight);

    public void OnQuestionStart(bool isDefaultType) => _player.OnQuestionStart();

    public void Report(string report) => _player.SendReport();

    public void ReceiveText(Message m)
    {
        // Do nothing
    }

    public void Stage()
    {
        // Do nothing
    }

    public void RoundThemes(bool print)
    {
        // Do nothing
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        if (themeIndex < 0 ||
            themeIndex >= _data.TInfo.RoundInfo.Count ||
            questionIndex < 0 ||
            questionIndex >= _data.TInfo.RoundInfo[themeIndex].Questions.Count)
        {
            return;
        }

        _data.TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = Question.InvalidPrice;

        if (_role == GameRole.Player)
        {
            _player.OnQuestionSelected();
        }
    }

    public void OnRightAnswer(string answer) { }

    public void Resume() { }

    virtual public void EndTry(string text)
    {
        // Do nothing
    }

    public void ShowTablo()
    {
        // Do nothing
    }

    public void StopRound()
    {

    }

    public void Out(int themeIndex) => _data.TInfo.RoundInfo[themeIndex].Name = "";

    public void TimeOut()
    {

    }

    public void FinalThink()
    {

    }

    public void UpdatePicture(int i, string path)
    {

    }

    public void UpdatePicture(Account account, string path)
    {

    }

    public void OnTextSpeed(double speed)
    {

    }

    public void OnPauseChanged(bool isPaused)
    {
        _data.TInfo.Pause = isPaused;
    }

    public void TableLoaded()
    {

    }

    public void PrintGreeting()
    {

    }

    public void OnTimeChanged()
    {

    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null)
    {
        switch (timerCommand)
        {
            case MessageParams.Timer_Go:
                var maxTime = int.Parse(arg);
                var now = DateTime.UtcNow;
                _timersInfo[timerIndex].IsEnabled = true;
                _timersInfo[timerIndex].StartTime = now;
                _timersInfo[timerIndex].EndTime = now.AddMilliseconds(maxTime * 100);
                _timersInfo[timerIndex].MaxTime = maxTime;

                break;

            case MessageParams.Timer_Stop:
                _timersInfo[timerIndex].IsEnabled = false;
                _timersInfo[timerIndex].PauseTime = -1;
                break;

            case MessageParams.Timer_Pause:
                var currentTime = int.Parse(arg);
                _timersInfo[timerIndex].IsEnabled = false;
                _timersInfo[timerIndex].PauseTime = currentTime;
                break;

            case MessageParams.Timer_UserPause:
                var currentTime2 = int.Parse(arg);
                _timersInfo[timerIndex].IsUserEnabled = false;

                if (_timersInfo[timerIndex].IsEnabled)
                {
                    _timersInfo[timerIndex].PauseTime = currentTime2;
                }

                break;

            case MessageParams.Timer_Resume:
                _timersInfo[timerIndex].IsEnabled = true;

                if (!_timersInfo[timerIndex].IsUserEnabled)
                {
                    return;
                }

                var now2 = DateTime.UtcNow;
                _timersInfo[timerIndex].EndTime = now2.AddMilliseconds((_timersInfo[timerIndex].MaxTime - _timersInfo[timerIndex].PauseTime) * 100);
                _timersInfo[timerIndex].StartTime = _timersInfo[timerIndex].EndTime.AddMilliseconds(-_timersInfo[timerIndex].MaxTime * 100);

                break;

            case MessageParams.Timer_UserResume:
                _timersInfo[timerIndex].IsUserEnabled = true;

                if (!_timersInfo[timerIndex].IsEnabled || _timersInfo[timerIndex].PauseTime == -1)
                {
                    return;
                }

                var now3 = DateTime.UtcNow;
                _timersInfo[timerIndex].EndTime = now3.AddMilliseconds((_timersInfo[timerIndex].MaxTime - _timersInfo[timerIndex].PauseTime) * 100);
                _timersInfo[timerIndex].StartTime = _timersInfo[timerIndex].EndTime.AddMilliseconds(-_timersInfo[timerIndex].MaxTime * 100);

                break;

            case MessageParams.Timer_MaxTime:
                var maxTime2 = int.Parse(arg);
                _timersInfo[timerIndex].MaxTime = maxTime2;
                break;
        }
    }

    public void OnPersonFinalStake(int playerIndex)
    {

    }

    public void OnPersonPass(int playerIndex)
    {

    }

    public void OnReplic(string personCode, string text)
    {

    }

    public void AddLog(string message) => _player.AddLog(message);

    public void SelectQuestion() => _player.SelectQuestion();

    public void DeleteTheme() => _player.DeleteTheme();

    public void OnInfo()
    {
        if (_role == GameRole.Showman)
        {
            _showman.OnInitialized();
        }
        else
        {
            _player.OnInitialized();
        }
    }

    public void ValidateAnswer(int playerIndex, string answer)
    {
        var isRight = AnswerChecker.IsAnswerRight(answer, _data.Right);
        _viewerActions.SendMessage(Messages.Validate, answer, isRight ? "+" : "-");
    }

    public void MakeStake() => _player.MakeStake();

    public void OnPersonStake(int stakerIndex) => _player.OnPersonStake(stakerIndex);

    public void OnPersonsUpdated() => _player.OnPersonsUpdated();
}
