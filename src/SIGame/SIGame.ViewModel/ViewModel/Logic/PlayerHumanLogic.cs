using SIData;
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

    public PlayerHumanLogic(
        ViewerData data,
        TableInfoViewModel tableInfoViewModel,
        ViewerActions viewerActions)
    {
        _viewerActions = viewerActions;
        _data = data;
        TInfo = tableInfoViewModel;

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
        _data.Host.OnFlash();
    }

    public void PersonAnswered(int playerIndex, bool isRight)
    {
        if ((_data.Stage == GameStage.Final || _data.QuestionType != QuestionTypes.Simple)
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
        _data.Host.OnFlash();

        if (TInfo.LayoutMode == LayoutMode.Simple)
        {
            _data.DialogMode = DialogModes.Answer;
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
            var version = _data.PersonDataExtensions.Answer;

            do
            {
                await Task.Delay(3000, cancellationToken);

                if (_data.PersonDataExtensions.Answer != version)
                {
                    _data.PlayerDataExtensions.SendAnswerVersion.Execute(null);
                    version = _data.PersonDataExtensions.Answer;
                }
            } while (_data.DialogMode == DialogModes.Answer && !cancellationToken.IsCancellationRequested);
        }
        catch
        {
            // Ignore
        }
    }

    public void Cat() => _data.Host.OnFlash();

    public void Stake()
    {
        _data.DialogMode = DialogModes.Stake;
        _data.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        ((PlayerAccount)_data.Me).IsDeciding = false;
        _data.Host.OnFlash();
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
        _data.Host.OnFlash();
    }

    public void CatCost()
    {
        _data.Hint = _viewerActions.LO[nameof(R.HintChooseCatPrice)];
        _data.DialogMode = DialogModes.CatCost;
        ((PlayerAccount)_data.Me).IsDeciding = false;

        _data.Host.OnFlash();
    }

    public void IsRight(bool voteForRight)
    {
        _data.Host.OnFlash();
    }

    #endregion

    public void Report()
    {
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
