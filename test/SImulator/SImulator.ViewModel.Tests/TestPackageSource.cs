using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Tests;

internal sealed class TestPackageSource : IPackageSource
{
    public string Name => throw new NotImplementedException();

    public string Token => "";

    public Task<(string filePath, bool isTemporary)> GetPackageFileAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult((Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "1.siq"), false));
}
