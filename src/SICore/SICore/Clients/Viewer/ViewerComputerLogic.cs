using System;
using SICore.Connections;
using SICore.Utils;
using SIData;

namespace SICore
{
    /// <summary>
    /// Логика зрителя-компьютера
    /// </summary>
    internal abstract class ViewerComputerLogic<C> : Logic<C, ViewerData>, IViewer
        where C : IViewerClient
    {
        protected sealed class TimerInfo
        {
            public bool IsEnabled { get; set; }
            public bool IsUserEnabled { get; set; } = true;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int MaxTime { get; set; }
            public int PauseTime { get; set; } = -1;
        }

        protected TimerInfo[] timersInfo = new TimerInfo[] { new TimerInfo(), new TimerInfo(), new TimerInfo() };

        protected int GetTimePercentage(int timerIndex)
        {
            var now = DateTime.Now;

            var timer = timersInfo[timerIndex];
            if (!timer.IsEnabled)
            {
                return timer.PauseTime > -1 ? 100 * timer.PauseTime / timer.MaxTime : 0;
            }

            return (int)(100 * (now - timer.StartTime).TotalMilliseconds / (timer.EndTime - timer.StartTime).TotalMilliseconds);
        }

        internal ViewerComputerLogic(C client, ViewerData data)
            : base(client, data)
        {
            
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
                _data.TInfo.RoundInfo[_data.ThemeIndex].Questions[_data.QuestionIndex].Price = QuestionHelper.InvalidQuestionPrice;
            }
        }

        virtual public void SetAtom(string[] mparams)
        {
            // Do nothing
        }

        virtual public void SetSecondAtom(string[] mparams)
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

        public void QType()
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

        public void SetText(string text)
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
                case "GO":
                    var maxTime = int.Parse(arg);
                    var now = DateTime.Now;
                    timersInfo[timerIndex].IsEnabled = true;
                    timersInfo[timerIndex].StartTime = now;
                    timersInfo[timerIndex].EndTime = now.AddMilliseconds(maxTime * 100);
                    timersInfo[timerIndex].MaxTime = maxTime;
                    break;

                case "STOP":
                    timersInfo[timerIndex].IsEnabled = false;
                    timersInfo[timerIndex].PauseTime = -1;
                    break;

                case "PAUSE":
                    var currentTime = int.Parse(arg);

                    timersInfo[timerIndex].IsEnabled = false;
                    timersInfo[timerIndex].PauseTime = currentTime;
                    break;

                case "USER_PAUSE":
                    var currentTime2 = int.Parse(arg);

                    timersInfo[timerIndex].IsUserEnabled = false;
                    if (timersInfo[timerIndex].IsEnabled)
                    {
                        timersInfo[timerIndex].PauseTime = currentTime2;
                    }
                    break;

                case "RESUME":
                    timersInfo[timerIndex].IsEnabled = true;
                    if (!timersInfo[timerIndex].IsUserEnabled)
                    {
                        return;
                    }

                    var now2 = DateTime.Now;
                    timersInfo[timerIndex].EndTime = now2.AddMilliseconds((timersInfo[timerIndex].MaxTime - timersInfo[timerIndex].PauseTime) * 100);
                    timersInfo[timerIndex].StartTime = timersInfo[timerIndex].EndTime.AddMilliseconds(-timersInfo[timerIndex].MaxTime * 100);
                    break;

                case "USER_RESUME":
                    timersInfo[timerIndex].IsUserEnabled = true;
                    if (!timersInfo[timerIndex].IsEnabled || timersInfo[timerIndex].PauseTime == -1)
                    {
                        return;
                    }

                    var now3 = DateTime.Now;
                    timersInfo[timerIndex].EndTime = now3.AddMilliseconds((timersInfo[timerIndex].MaxTime - timersInfo[timerIndex].PauseTime) * 100);
                    timersInfo[timerIndex].StartTime = timersInfo[timerIndex].EndTime.AddMilliseconds(-timersInfo[timerIndex].MaxTime * 100);
                    break;

                case "MAXTIME":
                    var maxTime2 = int.Parse(arg);
                    timersInfo[timerIndex].MaxTime = maxTime2;
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

        public SIUI.ViewModel.TableInfoViewModel TInfo
        {
            get { return null; }
        }
    }
}
