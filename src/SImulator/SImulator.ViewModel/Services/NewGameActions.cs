using SICore;
using SIData;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using Utils.Timers;

namespace SImulator.ViewModel.Services;

/// <summary>
/// Provides new version of game actions.
/// </summary>
internal sealed class NewGameActions : IGameActions, ITaskRunHandler<Model.Tasks>
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _state;
    private readonly Func<string, bool, GameRole, string?, AuthenticationResult> _join;
    private readonly Func<Message, ValueTask> _onMessageReceivedAsync;
    private readonly TaskRunner<Model.Tasks> _taskRunner;

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
        _taskRunner = new TaskRunner<Model.Tasks>(this);
    }

    public void ExecuteTask(Model.Tasks taskId, int arg)
    {
        _taskRunner.ScheduleExecution(Model.Tasks.NoTask, 0, runTimer: false);

        switch (taskId)
        {
            case Model.Tasks.MoveNext:
                _viewerActions.Move();
                break;
        }
    }

    public void Init() => _viewerActions.GetInfo();

    public void OnRightAnswer()
    {
        throw new NotImplementedException();
    }

    public void MoveBack() => _viewerActions.Move(MoveDirections.Back);

    public void MoveBackRound() => _viewerActions.Move(MoveDirections.RoundBack);

    public void MoveNext(int delayMs = 100)
    {
        if (_state.Stage == GameStage.Before)
        {
            _viewerActions.Start();
        }

        _taskRunner.ScheduleExecution(Model.Tasks.MoveNext, delayMs / 100);
    }

    public void ShowThemes(string[] themeNames)
    {

    }

    public void MoveNextRound() => _viewerActions.Move(MoveDirections.RoundNext);

    public void AddPlayer() => _viewerActions.AddTable();

    public void RemovePlayerAt(int index) => _viewerActions.RemoveTable(index);

    public void IsRightAnswer() => _viewerActions.IsRight(true);

    public void IsWrongAnswer() => _viewerActions.IsRight(false);

    public void SelectPlayer(int playerIndex) => _viewerActions.SelectPlayer(playerIndex);

    public void ConnectPlayer(PlayerInfo player) => _join(player.Name, false, GameRole.Player, null);

    public async void PlayerPressed(PlayerInfo player) => await _onMessageReceivedAsync(new Message("I", player.Name));

    public void Dispose()
    {

    }
}
