using SICore.Properties;
using System.Globalization;
using System.Resources;

namespace SICore.BusinessLogic
{
    public sealed class Localizer : ILocalizer
    {
        private readonly ResourceManager _resourceManager;

        private ResourceManager _packagesResourceManager;

        public CultureInfo Culture { get; }

        public Localizer(string culture)
        {
            _resourceManager = new ResourceManager("SICore.Properties.Resources", typeof(Resources).Assembly);
            Culture = new CultureInfo(culture ?? "en-US");
        }

        public string this[string key] => _resourceManager.GetString(key, Culture);

        public string GetPackagesString(string key)
        {
            _packagesResourceManager ??= new ResourceManager(
                "SIPackages.Properties.Resources",
                typeof(SIPackages.Properties.Resources).Assembly);

            return _packagesResourceManager.GetString(key, Culture);
        }
    }
}
