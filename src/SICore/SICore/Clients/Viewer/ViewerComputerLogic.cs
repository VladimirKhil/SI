using SIData;
using SIPackages;
using SIUI.ViewModel;

namespace SICore;

/// <summary>
/// Defines a computer viewer logic.
/// </summary>
internal class ViewerComputerLogic : Logic<ViewerData>, IViewerLogic
{
    protected readonly ViewerActions _viewerActions;

    public bool CanSwitchType => false;

    public IPlayerLogic PlayerLogic { get; }

    public IShowmanLogic ShowmanLogic { get; }

    protected sealed class TimerInfo
    {
        public bool IsEnabled { get; set; }

        public bool IsUserEnabled { get; set; } = true;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int MaxTime { get; set; }

        public int PauseTime { get; set; } = -1;
    }

    private readonly TimerInfo[] _timersInfo = new TimerInfo[] { new TimerInfo(), new TimerInfo(), new TimerInfo() };

    protected int GetTimePercentage(int timerIndex)
    {
        var now = DateTime.UtcNow;
        var timer = _timersInfo[timerIndex];

        if (!timer.IsEnabled)
        {
            return timer.PauseTime > -1 ? 100 * timer.PauseTime / timer.MaxTime : 0;
        }

        return (int)(100 * (now - timer.StartTime).TotalMilliseconds / (timer.EndTime - timer.StartTime).TotalMilliseconds);
    }

    internal ViewerComputerLogic(ViewerData data, ViewerActions viewerActions, ComputerAccount computerAccount)
        : base(data)
    {
        _viewerActions = viewerActions;

        //PlayerLogic = new PlayerComputerLogic(data, computerAccount, viewerActions);
        //ShowmanLogic = new ShowmanComputerLogic(data, viewerActions, computerAccount);
    }

    public void ReceiveText(Message m)
    {
        // Do nothing
    }

    public void Print(string text)
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

    virtual public void OnScreenContent(string[] mparams)
    {
        // Do nothing
    }

    virtual public void OnBackgroundContent(string[] mparams)
    {

    }

    public void SetRight(string answer) { }

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

    public virtual void Person(int playerIndex, bool isRight)
    {

    }

    public void OnQuestionType()
    {

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

    public void Winner()
    {

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

    public void TryConnect(IConnector connector)
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

    public void TextShape(string[] mparams)
    {
        
    }

    public void OnTimeChanged()
    {

    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string person)
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

            case "RESUME":
                _timersInfo[timerIndex].IsEnabled = true;

                if (!_timersInfo[timerIndex].IsUserEnabled)
                {
                    return;
                }

                var now2 = DateTime.UtcNow;
                _timersInfo[timerIndex].EndTime = now2.AddMilliseconds((_timersInfo[timerIndex].MaxTime - _timersInfo[timerIndex].PauseTime) * 100);
                _timersInfo[timerIndex].StartTime = _timersInfo[timerIndex].EndTime.AddMilliseconds(-_timersInfo[timerIndex].MaxTime * 100);
                
                break;

            case "USER_RESUME":
                _timersInfo[timerIndex].IsUserEnabled = true;

                if (!_timersInfo[timerIndex].IsEnabled || _timersInfo[timerIndex].PauseTime == -1)
                {
                    return;
                }

                var now3 = DateTime.UtcNow;
                _timersInfo[timerIndex].EndTime = now3.AddMilliseconds((_timersInfo[timerIndex].MaxTime - _timersInfo[timerIndex].PauseTime) * 100);
                _timersInfo[timerIndex].StartTime = _timersInfo[timerIndex].EndTime.AddMilliseconds(-_timersInfo[timerIndex].MaxTime * 100);
                
                break;

            case "MAXTIME":
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

    public TableInfoViewModel TInfo => null;
}
