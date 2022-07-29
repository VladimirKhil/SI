using System.Globalization;

namespace SICore.BusinessLogic
{
    public interface ILocalizer
    {
        string this[string key] { get; }

        CultureInfo Culture { get; }

        string GetPackagesString(string key);
    }
}
