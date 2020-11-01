using SICore.BusinessLogic;
using SICore.Properties;
using SIData;
using SIUI.ViewModel;
using System;
using System.Threading.Tasks;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Логика игрока-человека
    /// </summary>
    internal sealed class PlayerHumanLogic : ViewerHumanLogic, IPlayer
    {
        public PlayerHumanLogic(ViewerData data, ViewerActions viewerActions, ILocalizer localizer)
            : base(data, viewerActions, localizer)
        {
            TInfo.QuestionSelected += PlayerClient_QuestionSelected;
            TInfo.ThemeSelected += PlayerClient_ThemeSelected;

            TInfo.SelectQuestion.CanBeExecuted = false;
            TInfo.SelectTheme.CanBeExecuted = false;
        }

        #region PlayerInterface Members

        public void ChooseQuest()
        {
            lock (_data.ChoiceLock)
            {
                _data.ThemeIndex = _data.QuestionIndex = -1;
            }

            TInfo.Selectable = true;
            TInfo.SelectQuestion.CanBeExecuted = true;
            _data.BackLink.OnFlash();
        }

        public void PersonAnswered(int playerIndex, bool isRight)
        {
            if (_data.Stage == GameStage.Final && _data.Players[playerIndex].Name == _viewerActions.Client.Name || isRight)
            {
                _data.PlayerDataExtensions.Apellate.CanBeExecuted = _data.PlayerDataExtensions.NumApps > 0;
                _data.PlayerDataExtensions.Pass.CanBeExecuted = false;
            }
        }

        public void EndThink()
        {
            _data.PlayerDataExtensions.Pass.CanBeExecuted = false;
        }

        public void Answer()
        {
            _data.BackLink.OnFlash();
        }

        void SendAnswer(object sender, EventArgs e)
        {

        }

        public void Cat()
        {
            _data.BackLink.OnFlash();
        }

        public void Stake()
        {
            _data.DialogMode = DialogModes.Stake;
            _data.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
            ((PlayerAccount)ClientData.Me).IsDeciding = false;
            _data.BackLink.OnFlash();
        }

        public void ChooseFinalTheme()
        {
            _data.ThemeIndex = -1;

            TInfo.Selectable = true;
            TInfo.SelectTheme.CanBeExecuted = true;

            _data.BackLink.OnFlash();
        }

        public void FinalStake()
        {
            _data.BackLink.OnFlash();
        }

        public void CatCost()
        {
            _data.Hint = _viewerActions.LO[nameof(R.HintChooseCatPrice)];
            _data.DialogMode = DialogModes.CatCost;
            ((PlayerAccount)ClientData.Me).IsDeciding = false;

            _data.BackLink.OnFlash();
        }

        public void IsRight(bool voteForRight)
        {
            _data.BackLink.OnFlash();
        }

        public void Connected(string name)
        {

        }

        #endregion

        public void Report()
        {
            if (_data.BackLink.SendReport)
                _data.BackLink.OnFlash();
            else
            {
                var cmd = _data.SystemLog.Length > 0 ? _data.PlayerDataExtensions.Report.SendReport : _data.PlayerDataExtensions.Report.SendNoReport;
                if (cmd != null && cmd.CanExecute(null))
                    cmd.Execute(null);
            }
        }

        private async void Greet()
        {
            try
            {
                await Task.Delay(2000);

                AddLog(string.Format(_viewerActions.LO[nameof(R.Hint)], _data.BackLink.GameButtonKey));
                AddLog(_viewerActions.LO[nameof(R.PressButton)] + Environment.NewLine);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void OnInitialized()
        {
            Greet();
        }

        public void Table()
        {

        }

        private void PlayerClient_QuestionSelected(QuestionInfoViewModel question)
        {
            var found = false;
            for (var i = 0; i < TInfo.RoundInfo.Count; i++)
            {
                for (var j = 0; j < TInfo.RoundInfo[i].Questions.Count; j++)
                {
                    if (TInfo.RoundInfo[i].Questions[j] == question)
                    {
                        found = true;
                        _viewerActions.SendMessageWithArgs(Messages.Choice, i, j);
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            Clear();
        }

        private void PlayerClient_ThemeSelected(ThemeInfoViewModel theme)
        {
            for (int i = 0; i < TInfo.RoundInfo.Count; i++)
            {
                if (TInfo.RoundInfo[i] == theme)
                {
                    _viewerActions.SendMessageWithArgs(Messages.Delete, i);
                    break;
                }
            }

            Clear();
        }

        public void FinalThemes()
        {
            
        }

        public void Clear()
        {
            TInfo.Selectable = false;
            TInfo.SelectQuestion.CanBeExecuted = false;
            TInfo.SelectTheme.CanBeExecuted = false;
            _data.Hint = "";
            _data.DialogMode = DialogModes.None;

            for (int i = 0; i < _data.Players.Count; i++)
            {
                _data.Players[i].CanBeSelected = false;
            }

            _data.BackLink.OnFlash(false);
        }

        public void StartThink()
        {
            
        }
    }
}
