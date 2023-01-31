namespace SICore.PlatformSpecific;

/// <summary>
/// Provides a desktop-specific game logic.
/// </summary>
public sealed class DesktopCoreManager : CoreManager
{
    public override byte[]? GetData(string filename) => File.Exists(filename) ? File.ReadAllBytes(filename) : null;

    public override bool FileExists(string filePath) => File.Exists(filePath);

    public override Stream GetFile(string filePath) => File.OpenRead(filePath);
}
