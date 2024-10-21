using SICore.Properties;
using System.Globalization;
using System.Resources;

namespace SICore.BusinessLogic;

public sealed class Localizer : ILocalizer
{
    private readonly ResourceManager _resourceManager;

    public CultureInfo Culture { get; }

    public Localizer(string culture)
    {
        _resourceManager = new ResourceManager("SICore.Properties.Resources", typeof(Resources).Assembly);
        Culture = new CultureInfo(culture ?? "en-US");
    }

    public string this[string key] => _resourceManager.GetString(key, Culture);
}
