using System.Xml;

namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Represents chgk database client.
/// </summary>
public interface IChgkDbClient
{
    /// <summary>
    /// Service Uri.
    /// </summary>
    Uri? ServiceUri { get; }

    /// <summary>
    /// Gets XML file by name.
    /// </summary>
    /// <param name="name">File name.</param>
    /// <param name="cancellationToken">Cancelltion token.</param>
    Task<XmlDocument> GetXmlAsync(string name, CancellationToken cancellationToken);
}
