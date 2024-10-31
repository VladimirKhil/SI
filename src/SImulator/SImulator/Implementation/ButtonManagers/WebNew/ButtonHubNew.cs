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

    public ButtonHubNew(IGameRepository gameRepository) => _gameRepository = gameRepository;

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

        if (_gameRepository.BannedNames.Contains(request.UserName))
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.Forbidden };
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, request.GameId.ToString(), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SubscribersGroup, Context.ConnectionAborted);

        // Add connection
        _gameRepository.AddPlayer(Context.ConnectionId, request.UserName);
        Context.Items["userName"] = request.UserName;

        return JoinGameResponse.Success;
    }

    public void SendMessage(Message m)
    {
        var args = m.Text.Split('\n');
        
        if (Context.Items["userName"] is not string playerName || args.Length == 0)
        {
            return;
        }

        switch (args[0])
        {
            case "ANSWER":
                if (args.Length < 2)
                {
                    return;
                }

                var answer = args[1];
                _gameRepository.OnPlayerAnswer(playerName, answer, false);
                break;

            case "ANSWER_VERSION":
                if (args.Length < 2)
                {
                    return;
                }

                var answerVersion = args[1];
                _gameRepository.OnPlayerAnswer(playerName, answerVersion, true);
                break;

            case "I":
                _gameRepository.OnPlayerPress(playerName);
                break;

            case "INFO":
                _gameRepository.InformPlayer(playerName, Context.ConnectionId);
                break;

            case "PASS":
                _gameRepository.OnPlayerPass(playerName);
                break;

            default:
                break;
        }
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
        if (Context.Items["userName"] is string playerName)
        {
            _gameRepository.RemovePlayer(playerName);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveGame();
        await base.OnDisconnectedAsync(exception);
    }
}
