using SICore.Network.Contracts;
using SICore.Network.Properties;
using System.Globalization;
using System.Resources;

namespace SICore.Network;

public sealed class NetworkLocalizer : INetworkLocalizer
{
    private readonly ResourceManager _resourceManager;

    public CultureInfo Culture { get; }

    public NetworkLocalizer(string culture)
    {
        _resourceManager = new ResourceManager("SICore.Network.Properties.Resources", typeof(Resources).Assembly);
        Culture = new CultureInfo(culture ?? "ru-RU");
    }

    public string this[string key] => _resourceManager.GetString(key, Culture);
}
