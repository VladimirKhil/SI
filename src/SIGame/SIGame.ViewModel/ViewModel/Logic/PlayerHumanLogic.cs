using SICore.BusinessLogic;
using SIGame.ViewModel;
using SIPackages.Core;
using SIUI.ViewModel;
using System.Diagnostics;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Логика игрока-человека
/// </summary>
internal sealed class PlayerHumanLogic : IPlayerLogic, IDisposable
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public TableInfoViewModel TInfo { get; }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

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

        TInfo.SelectQuestion.CanBeExecuted = false;
        TInfo.SelectTheme.CanBeExecuted = false;
    }

    #region PlayerInterface Members

    public void PersonAnswered(int playerIndex, bool isRight)
    {
        if (_data.QuestionType != QuestionTypes.Simple
            && _data.Players[playerIndex].Name == _viewerActions.Client.Name
            || isRight)
        {
            _data.PlayerDataExtensions.Apellate.CanBeExecuted = _data.PlayerDataExtensions.ApellationCount > 0;
            _data.PlayerDataExtensions.Pass.CanBeExecuted = false;
        }
    }

    public void EndThink() => _data.PlayerDataExtensions.Pass.CanBeExecuted = false;

    public void Answer()
    {
        _gameViewModel.Answer = "";
        _data.Host.OnFlash();

        if (TInfo.LayoutMode == LayoutMode.Simple)
        {
            _gameViewModel.DialogMode = DialogModes.Answer;
            ((PlayerAccount)_data.Me).IsDeciding = false;

            StartSendingVersion(_cancellationTokenSource.Token);
        }
        else
        {
            TInfo.Selectable = true;
            TInfo.SelectAnswer.CanBeExecuted = true;
        }
    }

    /// <summary>
    /// Periodically sends player answer to server.
    /// </summary>
    private async void StartSendingVersion(CancellationToken cancellationToken)
    {
        try
        {
            var version = _gameViewModel.Answer;

            do
            {
                await Task.Delay(3000, cancellationToken);

                if (_gameViewModel.Answer != version)
                {
                    _gameViewModel.SendAnswerVersion.Execute(null);
                    version = _gameViewModel.Answer;
                }
            } while (_gameViewModel.DialogMode == DialogModes.Answer && !cancellationToken.IsCancellationRequested);
        }
        catch
        {
            // Ignore
        }
    }

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

    public void StakeNew()
    {
        _gameViewModel.SendStakeNew.CanBeExecuted = _data.PersonDataExtensions.Var[1];
        _gameViewModel.SendPassNew.CanBeExecuted = _data.PersonDataExtensions.Var[2];
        _gameViewModel.SendAllInNew.CanBeExecuted = _data.PersonDataExtensions.Var[3];

        _gameViewModel.DialogMode = DialogModes.StakeNew;
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        ((PlayerAccount)_data.Me).IsDeciding = false;
        _data.Host.OnFlash();
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

    public void IsRight(bool voteForRight, string answer)
    {
        _gameViewModel.Hint = _localizer[nameof(R.HintCheckAnswer)];
        _gameViewModel.DialogMode = DialogModes.AnswerValidation;
        _gameViewModel.Answer = answer;
        _data.Host.OnFlash();
    }

    #endregion

    public void Report()
    {
        _gameViewModel.DialogMode = DialogModes.Report;
        _data.Host.OnFlash();
    }

    private async void Greet()
    {
        try
        {
            await Task.Delay(2000);

            _data.OnAddString(null, string.Format(_viewerActions.LO[nameof(R.Hint)], _data.Host.GameButtonKey), LogMode.Log);
            _data.OnAddString(null, _viewerActions.LO[nameof(R.PressButton)] + Environment.NewLine, LogMode.Log);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception exc)
        {
            Trace.TraceError("Greet error: " + exc);
        }
    }

    public void OnInitialized() => Greet();

    public void StartThink()
    {
        
    }

    public void OnPlayerAtom(string[] mparams)
    {
        
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
