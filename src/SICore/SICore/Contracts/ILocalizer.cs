using System.Globalization;

namespace SICore.Contracts;

public interface ILocalizer
{
    string this[string key] { get; }

    CultureInfo Culture { get; }
}
