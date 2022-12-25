using SIPackages.Core;

namespace SICore;

/// <inheritdoc cref="ShareBase" />
public sealed class FilesManager : ShareBase, IFilesManager
{
    private readonly int _gameID = -1;
    private readonly int _multimediaPort = -1;
    private readonly string _rootPath;

    public FilesManager(int gameID, int multimediaPort, string rootPath)
    {
        _gameID = gameID;
        _multimediaPort = multimediaPort;
        _rootPath = rootPath;
    }

    public override string MakeUri(string file, string? category)
    {
        var uri = Uri.EscapeDataString(file);

        if (category != null && !string.IsNullOrEmpty(_rootPath))
        {
            return $"{_rootPath}/{category}/{uri}"; // The resource will be provided by the external server
        }

        return string.Format("http://localhost:{0}/data/{1}/{2}", _multimediaPort, _gameID, uri);
    }

    public StreamInfo? GetFile(string file)
    {
        Func<StreamInfo>? response;

        lock (_filesSync)
        {
            if (!_files.TryGetValue(Uri.UnescapeDataString(file), out response) && !_files.TryGetValue(file, out response))
            {
                return null;
            }
        }

        return response();
    }
}
