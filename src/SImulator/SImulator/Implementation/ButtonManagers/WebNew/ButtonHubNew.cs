using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Provides a SignalR Hub for web clients.
/// </summary>
public sealed class ButtonHubNew : Hub<IButtonClient>
{
    internal const string SubscribersGroup = "#subscribers";

    private readonly IGameRepository _gameRepository;

    public ButtonHubNew(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<GameInfo?> TryGetGameInfo(int gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SubscribersGroup, Context.ConnectionAborted);
        return _gameRepository.TryGetGameById(gameId);
    }

    public async Task<JoinGameResponse> JoinGame(JoinGameRequest request)
    {
        if (request.Role != GameRole.Player)
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.InvalidRole };
        }

        var info = _gameRepository.TryGetGameById(request.GameId);

        if (info == null)
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.GameNotFound };
        }

        // Add connection
        _gameRepository.AddPlayer(request.UserName);

        await Groups.AddToGroupAsync(Context.ConnectionId, request.GameId.ToString(), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SubscribersGroup, Context.ConnectionAborted);
        return JoinGameResponse.Success;
    }

    public void SendMessage(Message m)
    {
		// TODO
    }

    public async Task LeaveGame()
    {
        var gameId = 1;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
        var info = _gameRepository.TryGetGameById(gameId);

        if (info == null)
        {
            return;
        }

        // Remove connection
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveGame();
        await base.OnDisconnectedAsync(exception);
    }
}
