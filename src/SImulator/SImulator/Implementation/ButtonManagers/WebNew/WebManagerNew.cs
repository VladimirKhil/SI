using Microsoft.AspNetCore.Builder;
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
public sealed class WebManagerNew : ButtonManagerBase, IGameRepository, ICommandExecutor
{
    private readonly WebApplication _webApplication;

    private string _stageName = "Before";

    public ICollection<string> BannedNames { get; } = new HashSet<string>();

    public ConnectionPersonData[] Players => Listener.GamePlayers.Select(p => new ConnectionPersonData
    {
        Role = GameRole.Player,
        IsOnline = p.IsConnected,
        Name = p.Name
    }).ToArray();

    public WebManagerNew(int port, IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
            .AddSignalR(options => options.HandshakeTimeout = TimeSpan.FromMinutes(1))
            .AddMessagePackProtocol();
        
        builder.Services.AddSingleton<IGameRepository>(this);

        _webApplication = builder.Build();

        var fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot2"));

        _webApplication.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
            })
            .UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
            })
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapHub<ButtonHubNew>("/sihost"));

        _webApplication.MapGet("/api/v1/info/bots", () => Array.Empty<string>());
        
        _webApplication.MapGet("/api/v1/info/host", () => new 
        {
            ContentInfos = new[] { new { } },
            StorageInfos = Array.Empty<object>(),
        });

        _webApplication.RunAsync($"http://+:{port}");
    }

    public override bool ArePlayersManaged() => true;

    public override bool Start()
    {
        return true;
    }

    public override void Stop()
    {

    }

    public override ValueTask DisposeAsync() => _webApplication.DisposeAsync();

    public GameInfo? TryGetGameById(int gameId) => new()
    {
        GameID = 1,
        GameName = "SIGame",
        StartTime = DateTime.Now,
        Persons = Players,
        Rules = GameRules.FalseStart
    };

    public Task<bool> TryAddPlayerAsync(string id, string playerName) => UI.ExecuteAsync(
        () => Listener.TryConnectPlayer(playerName, id),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public Task<bool> TryRemovePlayerAsync(string playerName) => UI.ExecuteAsync(
        () => Listener.TryDisconnectPlayer(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public override ICommandExecutor? TryGetCommandExecutor() => this;

    public void OnStage(string stageName)
    {
        _stageName = stageName;
        SendMessage("STAGE", stageName);
    }

    private void SendMessageTo(string connectionId, params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Client(connectionId).Receive(message);
    }

    private void SendMessage(params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Group("1").Receive(message);
    }

    public void OnPlayerPress(string playerName) => UI.Execute(
        () => Listener.OnPlayerPressed(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerAnswer(string playerName, string answer, bool isPreliminary) => UI.Execute(
        () => Listener.OnPlayerAnswered(playerName, answer, isPreliminary),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerPass(string playerName) => UI.Execute(
        () => Listener.OnPlayerPassed(playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void OnPlayerStake(string playerName, int stake) => UI.Execute(
        () => Listener.OnPlayerStake(playerName, stake),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void InformPlayer(string playerName, string connectionId)
    {
        SendMessageTo(connectionId, "INFO2", "1", "", "+", "+", "+", "+", playerName, "+", "+", "+", "+");
        SendMessageTo(connectionId, "STAGE_INFO", _stageName, "", "-1");
    }

    public void AskStake(string connectionId, int maximum) => SendMessageTo(connectionId, "ASK_STAKE", "Stake", "1", maximum.ToString(), "1", "", "");

    public void AskTextAnswer() => SendMessage("ANSWER");

    public void AskOralAnswer() => SendMessage("ORAL_ANSWER");

    public void Cancel() => SendMessage("CANCEL");

    public override void OnPlayersChanged()
    {
        try
        {
            var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
            context.Clients.Group(ButtonHubNew.SubscribersGroup).GamePersonsChanged(1, Players);
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }

    public override void DisconnectPlayerById(string id, string name)
    {
        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Client(id).Disconnect();
        BannedNames.Add(name);
    }
}
