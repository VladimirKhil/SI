using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using SImulator.ViewModel.ButtonManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SImulator.Implementation.ButtonManagers.Web
{
    public sealed class WebManager2 : ButtonManagerBase
    {
        public static WebManager2 Current;

        private class Startup
        {
            public void Configuration(IAppBuilder appBuilder)
            {
                HttpConfiguration config = new HttpConfiguration();
                config.Routes.MapHttpRoute(
                    name: "Main",
                    routeTemplate: "{file}",
                    defaults: new { controller = "WebMain", action = "Get", file = "index.html" }
                );

                config.Routes.MapHttpRoute(
                    name: "Scripts",
                    routeTemplate: "Scripts/{file}",
                    defaults: new { controller = "WebMain", action = "GetScript" }
                );

                // Очень важно создавать новый Resolver, иначе повторное создание SignalR не будет работать
                GlobalHost.DependencyResolver = new DefaultDependencyResolver();
                appBuilder.MapSignalR("/host", new HubConfiguration());

                appBuilder.UseWebApi(config);
            }
        }

        readonly IDisposable _web;

        public WebManager2(int port)
        {
            var options = new StartOptions
            {
                ServerFactory = "Nowin",
                Port = port
            };

            _web = WebApp.Start<Startup>(options);
            Current = this;
        }

        public override bool Run()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ButtonHub>();
            context.Clients.All.StateChanged(1);

            return true;
        }

        public override void Stop()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ButtonHub>();
            context.Clients.All.StateChanged(0);
        }

        public override void Dispose()
        {
            _web.Dispose();
        }

        internal string Press(string connectionId)
        {
            var player = OnGetPlayerByGuid(Guid.Parse(connectionId), true);
            if (player != null)
                OnPlayerPressed(player);
            else
                player = OnGetPlayerByGuid(Guid.Parse(connectionId), false);

            if (player == null)
                return "";

            return player.Name;
        }
    }
}
