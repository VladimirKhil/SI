using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utils;

namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Provides web-based buttons.
/// </summary>
public sealed class WebManagerNew(WebApplication webApplication, IGameRepository gameRepository, IButtonManagerListener buttonManagerListener)
    : ButtonManagerBase(buttonManagerListener), ICommandExecutor
{
    internal static async Task<WebManagerNew> CreateAsync(int port, IButtonManagerListener buttonManagerListener)
    {
        var builder = WebApplication.CreateBuilder();

        // Configure the URL for the web application
        builder.WebHost.UseUrls($"http://+:{port}/");

        builder.Services
            .AddSignalR(options => options.HandshakeTimeout = TimeSpan.FromMinutes(1))
            .AddMessagePackProtocol();

        var gameRepository = new GameRepository(buttonManagerListener);

        builder.Services.AddSingleton<IGameRepository>(gameRepository);

        var webApplication = builder.Build();

        var fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot2"));

        webApplication.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
            })
            .UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
            })
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapHub<ButtonHubNew>("/sihost"));

        webApplication.MapGet("/api/v1/info/bots", () => Array.Empty<string>());

        webApplication.MapGet("/api/v1/info/host", () => new
        {
            ContentInfos = new[] { new { } },
            StorageInfos = Array.Empty<object>(),
        });

        await webApplication.StartAsync();

        return new WebManagerNew(webApplication, gameRepository, buttonManagerListener);
    }

    public override bool ArePlayersManaged() => true;

    public override bool Start()
    {
        return true;
    }

    public override void Stop()
    {
        
    }

    public override ValueTask DisposeAsync() => webApplication.DisposeAsync();

    public override ICommandExecutor? TryGetCommandExecutor() => this;

    public void OnStage(string stageName)
    {
        gameRepository.StageName = stageName;
        SendMessage("STAGE", stageName);
    }

    private void SendMessageTo(string connectionId, params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        var context = webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Client(connectionId).Receive(message);
    }

    private void SendMessage(params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        var context = webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Group("1").Receive(message);
    }

    public void AskStake(string connectionId, int maximum) => SendMessageTo(connectionId, "ASK_STAKE", "Stake", "1", maximum.ToString(), "1", "", "");

    public void AskTextAnswer() => SendMessage("ANSWER");

    public void AskOralAnswer(string connectionId) => SendMessageTo(connectionId, "ORAL_ANSWER");

    public void Cancel() => SendMessage("CANCEL");

    public override void OnPlayersChanged()
    {
        try
        {
            var context = webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
            context.Clients.Group(ButtonHubNew.SubscribersGroup).GamePersonsChanged(1, gameRepository.Players);
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }

    public override void DisconnectPlayerById(string id, string name)
    {
        var context = webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Client(id).Disconnect();
        gameRepository.BannedNames.Add(name);
    }
}

internal sealed class GameRepository(IButtonManagerListener listener) : IGameRepository
{
    public ConnectionPersonData[] Players => [.. listener.GamePlayers.Select(p => new ConnectionPersonData
        {
            Role = GameRole.Player,
            IsOnline = p.IsConnected,
            Name = p.Name
        })];

    public ICollection<string> BannedNames { get; } = new HashSet<string>();

    public string StageName { get; set; } = "Before";

    public GameInfo? TryGetGameById(int gameId) => new()
    {
        GameID = 1,
        GameName = "SIGame",
        StartTime = DateTime.Now,
        Persons = Players,
        Rules = GameRules.FalseStart
    };

    public Task<bool> TryAddPlayerAsync(string id, string playerName) => UI.ExecuteAsync(
        () => listener.TryConnectPlayer(playerName, id),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public Task<bool> TryRemovePlayerAsync(string playerName) => UI.ExecuteAsync(
        () => listener.TryDisconnectPlayer(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerPress(string playerName) => UI.Execute(
        () => listener.OnPlayerPressed(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerAnswer(string playerName, string answer, bool isPreliminary) => UI.Execute(
        () => listener.OnPlayerAnswered(playerName, answer, isPreliminary),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerPass(string playerName) => UI.Execute(
        () => listener.OnPlayerPassed(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerStake(string playerName, int stake) => UI.Execute(
        () => listener.OnPlayerStake(playerName, stake),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));
}