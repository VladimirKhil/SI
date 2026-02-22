using SICore;
using SIData;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.Services;

/// <summary>
/// Provides new version of game actions.
/// </summary>
internal sealed class NewGameActions : IGameActions
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _state;
    private readonly Func<string, bool, GameRole, string?, AuthenticationResult> _join;
    private readonly Func<Message, ValueTask> _onMessageReceivedAsync;

    public NewGameActions(
        ViewerActions viewerActions,
        ViewerData state,
        Func<string, bool, GameRole, string?, AuthenticationResult> join,
        Func<Message, ValueTask> onMessageReceivedAsync)
    {
        _viewerActions = viewerActions;
        _state = state;
        _join = join;
        _onMessageReceivedAsync = onMessageReceivedAsync;
    }

    public void OnRightAnswer()
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
        if (_state.Stage == GameStage.Before)
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

    public void AddPlayer() => _viewerActions.AddTable();

    public void RemovePlayerAt(int index) => _viewerActions.RemoveTable(index);

    public void IsRightAnswer() => _viewerActions.IsRight(true);

    public void IsWrongAnswer() => _viewerActions.IsRight(false);

    public void SelectPlayer(int playerIndex) => _viewerActions.SelectPlayer(playerIndex);

    public void ConnectPlayer(PlayerInfo player) => _join(player.Name, false, GameRole.Player, null);

    public async void PlayerPressed(PlayerInfo player) => await _onMessageReceivedAsync(new Message("I", player.Name));
}
