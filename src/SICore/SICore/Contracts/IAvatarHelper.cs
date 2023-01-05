namespace SICore.Contracts;

/// <summary>
/// Provides helper methods for working with avatars.
/// </summary>
public interface IAvatarHelper
{
    /// <summary>
    /// Checks if avatar file exists.
    /// </summary>
    /// <param name="fileName">Avatar file name.</param>
    bool FileExists(string fileName);

    /// <summary>
    /// Extracts Base64-encoded data info avatar file.
    /// </summary>
    /// <param name="base64data">Base64-encoded data.</param>
    /// <param name="fileName">Avatar file name.</param>
    string? ExtractAvatarData(string base64data, string fileName);

    /// <summary>
    /// Adds exising file as the avatar.
    /// </summary>
    /// <param name="sourceFilePath">Existing file path.</param>
    /// <param name="fileName">Avatar file name.</param>
    void AddFile(string sourceFilePath, string fileName);
}
