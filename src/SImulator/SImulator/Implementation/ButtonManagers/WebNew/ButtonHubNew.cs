using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Provides a SignalR Hub for web clients.
/// </summary>
public sealed class ButtonHubNew(IGameRepository gameRepository) : Hub<IButtonClient>
{
    internal const string SubscribersGroup = "#subscribers";

    public async Task<GameInfo?> TryGetGameInfo(int gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SubscribersGroup, Context.ConnectionAborted);
        return gameRepository.TryGetGameById(gameId);
    }

    public async Task<JoinGameResponse> JoinGame(JoinGameRequest request)
    {
        if (request.Role != GameRole.Player)
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.InvalidRole };
        }

        var info = gameRepository.TryGetGameById(request.GameId);

        if (info == null)
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.GameNotFound };
        }

        if (gameRepository.BannedNames.Contains(request.UserName))
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.Forbidden };
        }

        if (!(await gameRepository.TryAddPlayerAsync(Context.ConnectionId, request.UserName)))
        {
            return new JoinGameResponse { ErrorType = JoinGameErrorType.InvalidRole };
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, request.GameId.ToString(), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SubscribersGroup, Context.ConnectionAborted);
        await Clients.Group(SubscribersGroup).GamePersonsChanged(request.GameId, gameRepository.Players);

        // Add connection
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
                gameRepository.OnPlayerAnswer(playerName, answer, false);
                break;

            case "ANSWER_VERSION":
                if (args.Length < 2)
                {
                    return;
                }

                var answerVersion = args[1];
                gameRepository.OnPlayerAnswer(playerName, answerVersion, true);
                break;

            case "I":
                gameRepository.OnPlayerPress(playerName);
                break;

            case "INFO":
                SendMessageTo("INFO2", "1", "", "+", "+", "+", "+", playerName, "+", "+", "+", "+");
                SendMessageTo("STAGE_INFO", gameRepository.StageName, "", "-1");
                break;

            case "PASS":
                gameRepository.OnPlayerPass(playerName);
                break;

            case "SET_STAKE":
                if (args.Length < 2 || !int.TryParse(args[2], out var stake))
                {
                    return;
                }

                gameRepository.OnPlayerStake(playerName, stake);
                break;

            default:
                break;
        }
    }

    private void SendMessageTo(params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        Clients.Caller.Receive(message);
    }

    public async Task LeaveGame()
    {
        var gameId = 1;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
        var info = gameRepository.TryGetGameById(gameId);

        if (info == null)
        {
            return;
        }

        // Remove connection
        if (Context.Items["userName"] is string playerName)
        {
            if (await gameRepository.TryRemovePlayerAsync(playerName))
            {
                await Clients.Group(SubscribersGroup).GamePersonsChanged(gameId, gameRepository.Players);
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await LeaveGame();
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception exc)
        {
            Console.WriteLine(exc.ToString());
        }
    }
}
