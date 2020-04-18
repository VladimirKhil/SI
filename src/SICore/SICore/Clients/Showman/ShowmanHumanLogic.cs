using SICore.Properties;
using SIUI.ViewModel;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Логика ведущего-человека
    /// </summary>
    internal sealed class ShowmanHumanLogic : ViewerHumanLogic<Showman>, IShowman
    {
        public ShowmanHumanLogic(Showman client, ViewerData data)
            : base(client, data)
        {
            TInfo.QuestionSelected += PlayerClient_QuestionSelected;
            TInfo.ThemeSelected += PlayerClient_ThemeSelected;

            TInfo.SelectQuestion.CanBeExecuted = false;
            TInfo.SelectTheme.CanBeExecuted = false;
        }

        #region IShowman Members

        public void StarterChoose()
        {
            _data.BackLink.OnFlash();
        }

        public void FirstStake()
        {
            _data.BackLink.OnFlash();
        }

        public void IsRight()
        {
            _data.BackLink.OnFlash();
        }

        public void FirstDelete()
        {
            _data.BackLink.OnFlash();
        }

        #endregion

        public void OnInitialized()
        {

        }

        public void ClearSelections(bool full = false)
        {
            if (full)
            {
                TInfo.Selectable = false;
                TInfo.SelectQuestion.CanBeExecuted = false;
                TInfo.SelectTheme.CanBeExecuted = false;
            }

            _data.Hint = "";
            _data.DialogMode = DialogModes.None;

            for (int i = 0; i < _data.Players.Count; i++)
            {
                _data.Players[i].CanBeSelected = false;
            }

            _data.BackLink.OnFlash(false);
        }

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

        public void Cat()
        {
            _data.BackLink.OnFlash();
        }

        public void Stake()
        {
            _data.DialogMode = DialogModes.Stake;
            _data.Hint = _actor.LO[nameof(R.HintMakeAStake)];
            _data.BackLink.OnFlash();

			foreach (var player in _data.Players)
			{
				player.IsDeciding = false;
			}
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
            
        }

        public void CatCost()
        {
            _data.Hint = _actor.LO[nameof(R.HintChooseCatPrice)];
            _data.DialogMode = DialogModes.CatCost;

			foreach (var player in _data.Players)
			{
				player.IsDeciding = false;
			}

			_data.BackLink.OnFlash();
        }

        public void Table()
        {
            
        }

        public void FinalThemes()
        {
            
        }

        void PlayerClient_QuestionSelected(QuestionInfoViewModel question)
        {
            for (int i = 0; i < TInfo.RoundInfo.Count; i++)
            {
                for (int j = 0; j < TInfo.RoundInfo[i].Questions.Count; j++)
                {
                    if (TInfo.RoundInfo[i].Questions[j] == question)
                    {
                        _actor.SendMessageWithArgs(Messages.Choice, i, j);
                    }
                }
            }

            ClearSelections();
        }

        void PlayerClient_ThemeSelected(ThemeInfoViewModel theme)
        {
            for (int i = 0; i < TInfo.RoundInfo.Count; i++)
            {
                if (TInfo.RoundInfo[i] == theme)
                {
                    _actor.SendMessageWithArgs(Messages.Delete, i);
                }
            }

            ClearSelections();
        }
    }
}
