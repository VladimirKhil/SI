using SICore.Contracts;
using SICore.Models;
using SIData;
using SIPackages;

namespace SICore;

/// <summary>
/// Defines a computer viewer logic.
/// </summary>
internal class ViewerComputerLogic : Logic<ViewerData>, IViewerLogic
{
    protected readonly ViewerActions _viewerActions;

    public bool CanSwitchType => false;

    private readonly PlayerComputerLogic _player;
    private readonly ShowmanComputerLogic _showman;

    public IPersonLogic PlayerLogic => _player;

    public IPersonLogic ShowmanLogic => _showman;

    private readonly TimerInfo[] _timersInfo = new TimerInfo[] { new(), new(), new() };

    private readonly GameRole _role;

    internal ViewerComputerLogic(ViewerData data, ViewerActions viewerActions, ComputerAccount computerAccount, IIntelligence intelligence, GameRole role)
        : base(data)
    {
        _viewerActions = viewerActions;
        _role = role;

        _player = new PlayerComputerLogic(data, computerAccount, intelligence, viewerActions, _timersInfo);
        _showman = new ShowmanComputerLogic(data, viewerActions, computerAccount);
    }

    public void StarterChoose() => _showman.StarterChoose();

    public void FirstStake() => _showman.FirstStake();

    public void FirstDelete() => _showman.FirstDelete();

    public void IsRight(bool voteForRight, string answer)
    {
        if (_role == GameRole.Showman)
        {
            _showman.IsRight(answer);
        }
        else
        {
            _player.IsRight(voteForRight);
        }
    }

    public void SelectPlayer()
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

    public void OnTheme(string[] mparams) => _player.OnTheme(mparams);

    public void OnQuestionSelected() => _player.OnQuestionSelected();

    public void StartThink() => _player.StartThink();

    public void EndThink() => _player.EndThink();

    public void Answer() => _player.Answer();

    public void OnPlayerOutcome(int playerIndex, bool isRight) => _player.PersonAnswered(playerIndex, isRight);

    public void OnQuestionContent() => _player.OnQuestionContent();

    public void Report(string report) => _player.Report();

    public void ReceiveText(Message m)
    {
        // Do nothing
    }

    public void Stage()
    {
        // Do nothing
    }

    public void GameThemes()
    {
        // Do nothing
    }

    public void RoundThemes(bool print)
    {
        // Do nothing
    }

    public void Choice()
    {
        lock (_data.ChoiceLock)
            lock (_data.TInfoLock)
            {
                _data.TInfo.RoundInfo[_data.ThemeIndex].Questions[_data.QuestionIndex].Price = Question.InvalidPrice;
            }
    }

    public void OnRightAnswer(string answer) { }

    public void Resume() { }

    virtual public void Try()
    {
        // Do nothing
    }

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

    public void Out(int themeIndex)
    {
        lock (_data.ChoiceLock)
            lock (_data.TInfoLock)
            {
                _data.TInfo.RoundInfo[themeIndex].Name = null;
            }
    }

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

    public void SetText(string text, TableStage tableStage)
    {

    }

    public void OnPauseChanged(bool isPaused)
    {
        ClientData.TInfo.Pause = isPaused;
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

    public void OnPersonFinalAnswer(int playerIndex)
    {

    }

    public void OnPackageLogo(string uri)
    {

    }

    public void OnPersonApellated(int playerIndex)
    {

    }

    public void OnPersonPass(int playerIndex)
    {

    }

    public void OnReplic(string personCode, string text)
    {

    }

    public void SelectQuestion() => _player.ChooseQuest();

    public void DeleteTheme() => _player.ChooseFinalTheme();

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
        var isRight = AnswerChecker.IsAnswerRight(answer, _data.PersonDataExtensions.Right);
        _viewerActions.SendMessage(Messages.Validate, answer, isRight ? "+" : "-");
    }
}
