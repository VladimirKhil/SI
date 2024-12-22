using SICore.BusinessLogic;
using SIGame.ViewModel;
using SIGame.ViewModel.Models;
using SIUI.ViewModel;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Логика игрока-человека
/// </summary>
[Obsolete]
internal sealed class PlayerHumanLogic : IPersonLogic
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public TableInfoViewModel TInfo { get; }

    private readonly GameViewModel _gameViewModel;
    private readonly ILocalizer _localizer;

    public PlayerHumanLogic(
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
    }

    #region PlayerInterface Members

    public void Cat()
    {
        _gameViewModel.Hint = _localizer[nameof(R.HintSelectCatPlayer)];
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
        ((PlayerAccount)_data.Me).IsDeciding = false;
        _data.Host.OnFlash();
    }

    public void FinalStake()
    {
        _gameViewModel.Hint = _localizer[nameof(R.HintMakeAStake)];
        _gameViewModel.DialogMode = DialogModes.FinalStake;
        _data.Host.OnFlash();
    }

    public void CatCost()
    {
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintChooseCatPrice)];
        _gameViewModel.DialogMode = DialogModes.CatCost;
        ((PlayerAccount)_data.Me).IsDeciding = false;

        _data.Host.OnFlash();
    }

    #endregion
}
