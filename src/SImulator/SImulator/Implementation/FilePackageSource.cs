using SImulator.ViewModel.PlatformSpecific;
using System.Threading;
using System.Threading.Tasks;

namespace SImulator.Implementation;

/// <summary>
/// Defines a file-based package source.
/// </summary>
internal sealed class FilePackageSource : IPackageSource
{
    public string Name { get; }

    public string Token => Name;

    public FilePackageSource(string path) => Name = path;

    public Task<(string filePath, bool isTemporary)> GetPackageFileAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((Name, false));

    public override string ToString() => Name;
}
