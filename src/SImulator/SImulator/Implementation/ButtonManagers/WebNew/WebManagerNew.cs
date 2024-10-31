using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.IO;
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

    public WebManagerNew(int port, IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
            .AddSignalR(options => options.HandshakeTimeout = TimeSpan.FromMinutes(1))
            .AddMessagePackProtocol();
        
        builder.Services.AddSingleton<IGameRepository>(this);

        _webApplication = builder.Build();

        _webApplication.UseDefaultFiles();
       
        _webApplication.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot2")),
        });

        _webApplication.UseRouting();

        _webApplication.UseEndpoints(endpoints => endpoints.MapHub<ButtonHubNew>("/sihost"));

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
        Persons = new ConnectionPersonData[]
        {
            new() { Role = GameRole.Player, IsOnline = false, Name = "" }
        },
        Rules = GameRules.FalseStart
    };

    public void AddPlayer(string id, string playerName) => UI.Execute(
        () => Listener.OnPlayerAdded(id, playerName),
        exc => PlatformManager.Instance.ShowMessage(exc.Message));

    public void RemovePlayer(string playerName) => UI.Execute(
        () => Listener.OnPlayerRemoved(playerName),
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

    public void InformPlayer(string playerName, string connectionId)
    {
        SendMessageTo(connectionId, "INFO2", "1", "", "+", "+", "+", "+", playerName, "+", "+", "+", "+");
        SendMessageTo(connectionId, "STAGE_INFO", _stageName, "", "-1");
    }

    public void AskTextAnswer() => SendMessage("ANSWER");

    public void Cancel() => SendMessage("CANCEL");

    public override void RemovePlayerById(string id, string name, bool manually = true)
    {
        if (!manually)
        {
            return;
        }

        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Client(id).Disconnect();
        BannedNames.Add(name);
    }
}
