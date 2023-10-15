using SIPackages;
using SIPackages.Providers;

namespace SICore.PlatformSpecific;

/// <summary>
/// Defines a folder-based package provider.
/// </summary>
public sealed class PackageProvider : IPackagesProvider
{
    private readonly string _folder;

    public PackageProvider(string folder)
    {
        _folder = folder;
    }

    public Task<IEnumerable<string>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        var dir = new DirectoryInfo(_folder);
        return Task.FromResult(dir.EnumerateFiles("*.siq").Select(file => file.Name));
    }

    public Task<SIDocument> GetPackageAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(SIDocument.Load(File.OpenRead(Path.Combine(_folder, name))));
}
