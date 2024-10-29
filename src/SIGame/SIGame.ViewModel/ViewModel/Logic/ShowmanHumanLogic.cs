using SICore.BusinessLogic;
using SIGame.ViewModel;
using SIGame.ViewModel.Models;
using SIUI.ViewModel;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Логика ведущего-человека
/// </summary>
internal sealed class ShowmanHumanLogic : IPersonLogic
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public TableInfoViewModel TInfo { get; }


    private readonly GameViewModel _gameViewModel;
    private readonly ILocalizer _localizer;

    public ShowmanHumanLogic(
        ViewerData data,
        TableInfoViewModel tableInfoViewModel,
        ViewerActions viewerActions,
        GameViewModel gameViewModel,
        ILocalizer localizer)
    {
        _viewerActions = viewerActions;
        _data = data;
        TInfo = tableInfoViewModel;
        _gameViewModel = gameViewModel;
        _localizer = localizer;

        TInfo.QuestionToggled += TInfo_QuestionToggled;

        TInfo.SelectQuestion.CanBeExecuted = false;
        TInfo.SelectTheme.CanBeExecuted = false;
    }

    public void OnInitialized()
    {

    }

    public void Cat()
    {
        _gameViewModel.Hint = _localizer[nameof(R.HintSelectCatPlayerForPlayer)];
        _data.Host.OnFlash();
    }

    public void Stake()
    {
        _gameViewModel.SendNominal.CanBeExecuted = _data.PersonDataExtensions.Var[0];
        _gameViewModel.SendStake.CanBeExecuted = _data.PersonDataExtensions.Var[1];
        _gameViewModel.SendPass.CanBeExecuted = _data.PersonDataExtensions.Var[2];
        _gameViewModel.SendVabank.CanBeExecuted = _data.PersonDataExtensions.Var[3];

        _gameViewModel.DialogMode = DialogModes.Stake;
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        _data.Host.OnFlash();

        foreach (var player in _data.Players)
        {
            player.IsDeciding = false;
        }
    }

    public void StakeNew()
    {
        _gameViewModel.SendStakeNew.CanBeExecuted = _data.PersonDataExtensions.Var[1];
        _gameViewModel.SendPassNew.CanBeExecuted = _data.PersonDataExtensions.Var[2];
        _gameViewModel.SendAllInNew.CanBeExecuted = _data.PersonDataExtensions.Var[3];

        _gameViewModel.DialogMode = DialogModes.StakeNew;
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        _data.Host.OnFlash();

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
        _gameViewModel.Hint = _localizer[nameof(R.HintSelectTheme)];

        _data.Host.OnFlash();
    }

    public void FinalStake()
    {
        
    }

    public void CatCost()
    {
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintChooseCatPrice)];
        _gameViewModel.DialogMode = DialogModes.CatCost;

        foreach (var player in _data.Players)
        {
            player.IsDeciding = false;
        }

        _data.Host.OnFlash();
    }

    private void TInfo_QuestionToggled(QuestionInfoViewModel question)
    {
        var found = false;

        for (var i = 0; i < TInfo.RoundInfo.Count; i++)
        {
            for (var j = 0; j < TInfo.RoundInfo[i].Questions.Count; j++)
            {
                if (TInfo.RoundInfo[i].Questions[j] == question)
                {
                    found = true;
                    _viewerActions.SendMessageWithArgs(Messages.Toggle, i, j);
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }
    }
}
