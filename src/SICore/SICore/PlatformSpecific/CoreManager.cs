namespace SICore.PlatformSpecific;

/// <summary>
/// Provides a system-specific game logic.
/// </summary>
public abstract class CoreManager
{
    internal static CoreManager Instance;

    protected CoreManager()
    {
        Instance = this;
    }

    public abstract byte[]? GetData(string filename);

    public abstract bool FileExists(string filePath);

    public abstract Stream GetFile(string filePath);
}
