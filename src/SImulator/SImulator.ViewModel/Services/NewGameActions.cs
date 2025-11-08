using SICore;
using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.Services;

/// <summary>
/// Provides new version of game actions.
/// </summary>
internal sealed class NewGameActions : IGameActions
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _state;

    public NewGameActions(ViewerActions viewerActions, ViewerData state)
    {
        _viewerActions = viewerActions;
        _state = state;
    }

    public void IsRightAnswer()
    {
        throw new NotImplementedException();
    }

    public void MoveBack()
    {

    }

    public void MoveBackRound()
    {
        throw new NotImplementedException();
    }

    public void MoveNext(int delayMs = 100)
    {
        if (_state.Stage == SIData.GameStage.Before)
        {
            _viewerActions.Start();
        }

        _viewerActions.Move();
    }

    public void ShowThemes(string[] themeNames)
    {

    }

    public void MoveNextRound()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        
    }
}
