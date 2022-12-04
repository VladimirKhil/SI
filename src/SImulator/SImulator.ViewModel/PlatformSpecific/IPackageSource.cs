namespace SImulator.ViewModel.PlatformSpecific;

/// <summary>
/// Allows to get game package file.
/// </summary>
public interface IPackageSource
{
    /// <summary>
    /// Package name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Unique package token which allows to recreate a link to the package in the future.
    /// </summary>
    string Token { get; }

    /// <summary>
    /// Tries go get the package file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Package file path and isTemporary marker or null.</returns>
    Task<(string filePath, bool isTemporary)> GetPackageFileAsync(CancellationToken cancellationToken = default);
}
