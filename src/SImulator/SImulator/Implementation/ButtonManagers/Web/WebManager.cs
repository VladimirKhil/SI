using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SImulator.ViewModel.ButtonManagers;
using System;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.Web;

/// <summary>
/// Provides web-based buttons.
/// </summary>
public sealed class WebManager : ButtonManagerBase, IButtonProcessor
{
    private readonly WebApplication _webApplication;

    public WebManager(int port)
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
        var player = !string.IsNullOrEmpty(token) ? OnGetPlayerById(token, true) : null;

        if (player != null)
        {
            OnPlayerPressed(player);
        }
        else
        {
            token = Guid.NewGuid().ToString();
            player = OnGetPlayerById(token, false);
        }

        return new PressResponse(player?.Name ?? "", token);
    }

    public override ValueTask DisposeAsync() => _webApplication.DisposeAsync();
}
