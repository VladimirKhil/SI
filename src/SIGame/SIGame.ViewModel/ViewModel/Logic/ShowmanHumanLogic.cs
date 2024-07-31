using SICore.BusinessLogic;
using SIUI.ViewModel;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Логика ведущего-человека
/// </summary>
internal sealed class ShowmanHumanLogic : IShowmanLogic
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public TableInfoViewModel TInfo { get; }

    public ShowmanHumanLogic(
        ViewerData data,
        TableInfoViewModel tableInfoViewModel,
        ViewerActions viewerActions)
    {
        _viewerActions = viewerActions;
        _data = data;
        TInfo = tableInfoViewModel;

        TInfo.QuestionToggled += TInfo_QuestionToggled;

        TInfo.SelectQuestion.CanBeExecuted = false;
        TInfo.SelectTheme.CanBeExecuted = false;
    }

    public void StarterChoose() => _data.Host.OnFlash();

    public void FirstStake() => _data.Host.OnFlash();

    public void IsRight() => _data.Host.OnFlash();

    public void FirstDelete() => _data.Host.OnFlash();

    public void OnInitialized()
    {

    }

    public void ChooseQuest()
    {
        lock (_data.ChoiceLock)
        {
            _data.ThemeIndex = _data.QuestionIndex = -1;
        }

        TInfo.Selectable = true;
        TInfo.SelectQuestion.CanBeExecuted = true;
        _data.Host.OnFlash();
    }

    public void Cat() => _data.Host.OnFlash();

    public void Stake()
    {
        _data.DialogMode = DialogModes.Stake;
        _data.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        _data.Host.OnFlash();

        foreach (var player in _data.Players)
        {
            player.IsDeciding = false;
        }
    }

    public void StakeNew()
    {
        _data.DialogMode = DialogModes.StakeNew;
        _data.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
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

        _data.Host.OnFlash();
    }

    public void FinalStake()
    {
        
    }

    public void CatCost()
    {
        _data.Hint = _viewerActions.LO[nameof(R.HintChooseCatPrice)];
        _data.DialogMode = DialogModes.CatCost;

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

    public void ManageTable(bool? mode) => TInfo.IsEditable = mode ?? !TInfo.IsEditable;

    public void Answer()
    {
        _data.Host.OnFlash();

        if (TInfo.LayoutMode != LayoutMode.Simple)
        {
            TInfo.Selectable = true;
            TInfo.SelectAnswer.CanBeExecuted = true;
        }
    }
}
