using Newtonsoft.Json;
using SICore;
using SICore.Network.Servers;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.ViewModel.Data;
using SIUI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SIGame.ViewModel
{
    public sealed class GameViewModel: IDisposable, INotifyPropertyChanged
    {
        private readonly Server _server;

        public IViewerClient Host { get; private set; }

        public ViewerData Data => Host.MyData;

        public TableInfoViewModel TInfo => Host.MyLogic.TInfo;

        public CustomCommand EndGame { get; }

        /// <summary>
        /// Задать/убрать паузу в игре
        /// </summary>
        public CustomCommand ChangePauseInGame { get; }

        public CustomCommand Move { get; set; }

        private bool _networkGame = false;

        public bool NetworkGame
        {
            get { return _networkGame; }
            set { _networkGame = value; OnPropertyChanged(); }
        }

        private int _networkGamePort;

        public int NetworkGamePort
        {
            get { return _networkGamePort; }
            set { _networkGamePort = value; OnPropertyChanged(); }
        }

        private bool _isPaused;

        public bool IsPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; OnPropertyChanged(); }
        }

        public event Action GameEnded;

        public CustomCommand Cancel { get; private set; }

        public bool IsOnline { get; set; }

        public string TempDocFolder { get; set; }

        public IAnimatableTimer[] Timers { get; } = new IAnimatableTimer[3];

        private string _ad;

        public string Ad
        {
            get { return _ad; }
            set { _ad = value; OnPropertyChanged(); }
        }

        public UserSettings UserSettings { get; }

        public double Volume
        {
            get { return TInfo.Volume * 100; }
            set { TInfo.Volume = Math.Max(1, value) / 100; }
        }

        public GameViewModel(Server server, IViewerClient host, UserSettings userSettings)
        {
            _server = server;

            _server.Reconnecting += Server_Reconnecting;
            _server.Reconnected += Server_Reconnected;

            Host = host ?? throw new ArgumentNullException(nameof(host));
            Host.Switch += Host_Switch;
            Host.StageChanged += UpdateMoveCommand;
            Host.PersonConnected += UpdateMoveCommand;
            Host.PersonDisconnected += UpdateMoveCommand;
            Host.Timer += Host_Timer;
            Host.Ad += Host_Ad;
            Host.IsPausedChanged += Host_IsPausedChanged;

            UserSettings = userSettings;

            ChangePauseInGame = new CustomCommand(ChangePauseInGame_Executed) { CanBeExecuted = false };
            Move = new CustomCommand(Move_Executed) { CanBeExecuted = false };
            EndGame = new CustomCommand(EndGame_Executed);
            Cancel = new CustomCommand(Cancel_Executed);

            for (int i = 0; i < Timers.Length; i++)
            {
                Timers[i] = PlatformManager.Instance.GetAnimatableTimer();
            }

            Timers[1].TimeChanged += GameViewModel_TimeChanged;
        }

        private void Host_IsPausedChanged(bool isPaused)
        {
            IsPaused = isPaused;
        }

        private void Server_Reconnected() => Host.AddLog(Resources.ReconnectingMessage);

        private void Server_Reconnecting() => Host.AddLog(Resources.ReconnectedMessage);

        private void Host_Ad(string text)
        {
            Ad = text;
            if (!string.IsNullOrEmpty(text))
            {
                TInfo.Text = "";
                TInfo.QuestionContentType = QuestionContentType.Text;
                TInfo.Sound = false;
                TInfo.TStage = TableStage.Question;
            }
        }

        private void GameViewModel_TimeChanged(IAnimatableTimer timer)
        {
            Host.MyLogic.TInfo.TimeLeft = timer.Time < 0.001 ? 0.0 : 1.0 - timer.Time / 100;
        }

        private void Host_Timer(int timerIndex, string timerCommand, string arg)
        {
            var timer = Timers[timerIndex];

            switch (timerCommand)
            {
                case "GO":
                    var maxTime = int.Parse(arg);
                    timer.Run(maxTime, false);
                    break;

                case "STOP":
                    timer.Stop();
                    break;

                case "PAUSE":
                    var currentTime = int.Parse(arg);
                    timer.Pause(currentTime, false);
                    break;

                case "USER_PAUSE":
                    var currentTime2 = int.Parse(arg);
                    timer.Pause(currentTime2, true);
                    break;

                case "RESUME":
                    timer.Run(-1, false);
                    break;

                case "USER_RESUME":
                    timer.Run(-1, true);
                    break;

                case "MAXTIME":
                    var maxTime2 = int.Parse(arg);
                    timer.MaxTime = maxTime2;
                    break;
            }
        }

        private void UpdateMoveCommand()
        {
            Move.CanBeExecuted = Data.Stage != SIData.GameStage.Before && (Host.IsHost || Host is Showman);
            ChangePauseInGame.CanBeExecuted = Move.CanBeExecuted;
        }

        private void Host_Switch(IViewerClient newHost)
        {
            newHost.Connector = Host.Connector;
            if (newHost.Connector != null)
            {
                newHost.Connector.SetHost(newHost);
            }

            Host.Switch -= Host_Switch;
            Host.StageChanged -= UpdateMoveCommand;
            Host.PersonConnected -= UpdateMoveCommand;
            Host.PersonDisconnected -= UpdateMoveCommand;
            Host.Timer -= Host_Timer;
            Host.Ad -= Host_Ad;
            Host.IsPausedChanged -= Host_IsPausedChanged;
            Host = newHost;
            Host.Switch += Host_Switch;
            Host.StageChanged += UpdateMoveCommand;
            Host.PersonConnected += UpdateMoveCommand;
            Host.PersonDisconnected += UpdateMoveCommand;
            Host.OnIsHostChanged += UpdateMoveCommand;
            Host.Timer += Host_Timer;
            Host.Ad += Host_Ad;
            Host.IsPausedChanged += Host_IsPausedChanged;

            UpdateMoveCommand();

            OnPropertyChanged(nameof(TInfo));
        }

        private void Move_Executed(object arg)
        {
            Host.Move(arg);
        }

        private void Cancel_Executed(object arg)
        {
            if (Host.MyLogic is IPerson logic)
            {
                ((ViewerData)logic.Data).DialogMode = DialogModes.None;
                ((ViewerData)logic.Data).Hint = "";
            }
        }

        private void ChangePauseInGame_Executed(object arg)
        {
            Host.Pause();
        }

        private void EndGame_Executed(object arg)
        {
            GameEnded?.Invoke();
        }

        public void Dispose()
        {
            _server.DisposeAsync();

            if (TempDocFolder != null && Directory.Exists(TempDocFolder))
            {
                try
                {
                    Directory.Delete(TempDocFolder, true);
                }
                catch (IOException exc)
                {
                    Trace.TraceWarning($"Temp folder delete error: {exc}");
                }
            }
        }

        /// <summary>
        /// Изменилось значение свойства
        /// </summary>
        /// <param name="name">Имя свойства</param>
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Init()
        {
            if (NetworkGame)
            {
                PrintNetworkInformation();
            }
            else if (IsOnline)
            {
                PrintOnlineInformation();
            }
        }

        private async void PrintOnlineInformation()
        {
            await Task.Delay(4000);

            Host.AddLog($"{Resources.OnlineGameAddress}: {CommonSettings.OnlineGameUrl}{Host.Connector.GameId}");
        }

        private async void PrintNetworkInformation()
        {
            var ips = new List<string>();

            try
            {
                var request = WebRequest.CreateHttp("https://api.ipify.org?format=json");
                var response = await request.GetResponseAsync();

                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var ipInfo = JsonSerializer.CreateDefault().Deserialize<IpResponse>(jsonReader);

                    if (ipInfo != null && ipInfo.Ip != null)
                    {
                        ips.Add($"{ipInfo.Ip}:{NetworkGamePort}");
                    }
                }
            }
            catch
            {

            }

            foreach (var ip in await Dns.GetHostAddressesAsync(Environment.MachineName))
            {
                ips.Add(ip.ToString() + ":" + NetworkGamePort);
            }

            await Task.Delay(2000);

            Host.AddLog($"IP:\n{string.Join("\n", ips)}");
        }
    }
}
