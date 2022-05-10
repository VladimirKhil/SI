using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SImulator.ViewModel.ButtonManagers;
using System;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.Web
{
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

            _webApplication.RunAsync($"http://localhost:{port}");
        }

        public override bool Run()
        {
            var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHub, IButtonClient>>();
            context.Clients.All.StateChanged(1); // No await

            return true;
        }

        public override void Stop()
        {
            var context = _webApplication.Services.GetRequiredService<IHubContext<ButtonHub, IButtonClient>>();
            context.Clients.All.StateChanged(0); // No await
        }

        public override ValueTask DisposeAsync() => _webApplication.DisposeAsync();

        public string Press(string connectionId)
        {
            var player = OnGetPlayerById(connectionId, true);
            if (player != null)
            {
                OnPlayerPressed(player);
            }
            else
            {
                player = OnGetPlayerById(connectionId, false);
            }

            return player?.Name ?? "";
        }
    }
}
