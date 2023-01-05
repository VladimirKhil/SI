using SICore.Clients;

namespace SICore.Contracts;

/// <summary>
/// Provides a method for sharing files.
/// </summary>
public interface IFileShare : IAsyncDisposable
{
    /// <summary>
    /// Creates an Uri to access the file.
    /// </summary>
    /// <param name="resourceKind">File resource kind.</param>
    /// <param name="relativePath">Relative path to the file.</param>
    /// <returns>Absolute Uri to access the file.</returns>
    Uri CreateResourceUri(ResourceKind resourceKind, Uri relativePath);
}
