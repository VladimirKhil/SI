using SICore.Clients;
using SICore.Contracts;

namespace SImulator.ViewModel.Services;

internal sealed class FileShare : IFileShare
{
    private readonly string _documentPath;

    public FileShare(string documentPath) => _documentPath = documentPath;

    public string CreateResourceUri(ResourceKind resourceKind, Uri relativePath) => Path.Combine(_documentPath, relativePath.ToString().TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
