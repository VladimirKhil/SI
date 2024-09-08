using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Provides web-based buttons.
/// </summary>
public sealed class WebManagerNew : ButtonManagerBase, IGameRepository, ICommandExecutor
{
    private readonly WebApplication _webApplication;
    private readonly System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

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

    public void AddPlayer(string userName) => _dispatcher.BeginInvoke(() => Listener.OnPlayerAdded(userName));

    public override ICommandExecutor? TryGetCommandExecutor() => this;

    public void OnStage(string stageName) => SendMessage("STAGE", stageName);

    private void SendMessage(params string[] args)
    {
        var messageText = string.Join("\n", args);
        var message = new Message { IsSystem = true, Sender = "@", Receiver = "*", Text = messageText };
        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHubNew, IButtonClient>>();
        context.Clients.Group("1").Receive(message);
    }
}
