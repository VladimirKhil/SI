using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.PlatformSpecific;
using System;
using System.Threading.Tasks;
using Utils;

namespace SImulator.Implementation.ButtonManagers.Web;

/// <summary>
/// Provides web-based buttons.
/// </summary>
public sealed class WebManager : ButtonManagerBase, IButtonProcessor
{
    private readonly WebApplication _webApplication;

    public WebManager(int port, IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IButtonProcessor>(this);

         _webApplication = builder.Build();

        _webApplication.UseDefaultFiles();
        _webApplication.UseStaticFiles();

        _webApplication.UseRouting();

        _webApplication.UseEndpoints(endpoints => endpoints.MapHub<ButtonHub>("/buttonHost"));

        _webApplication.RunAsync($"http://+:{port}");
    }

    public override bool Start()
    {
        var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHub, IButtonClient>>();
        context.Clients.All.StateChanged(1); // No await

        return true;
    }

    public override void Stop()
    {
        try
        {
            var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHub, IButtonClient>>();
            context.Clients.All.StateChanged(0); // No await
        }
        catch (ObjectDisposedException) { }
    }

    public PressResponse Press(string token)
    {
        var player = !string.IsNullOrEmpty(token) ? Listener.GetPlayerById(token, true) : null;

        if (player != null)
        {
            UI.Execute(() => Listener.OnPlayerPressed(player), exc => PlatformManager.Instance.ShowMessage(exc.Message));
        }
        else
        {
            token = Guid.NewGuid().ToString();
            player = Listener.GetPlayerById(token, false);
        }

        return new PressResponse(player?.Name ?? "", token, Listener.ButtonBlockTime);
    }

    public override ValueTask DisposeAsync() => _webApplication.DisposeAsync();
}
