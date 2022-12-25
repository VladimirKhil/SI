using System.Globalization;

namespace SICore.Network.Contracts;

/// <summary>
/// Supports localization for SI network objects.
/// </summary>
public interface INetworkLocalizer
{
    string this[string key] { get; }

    CultureInfo Culture { get; }
}
