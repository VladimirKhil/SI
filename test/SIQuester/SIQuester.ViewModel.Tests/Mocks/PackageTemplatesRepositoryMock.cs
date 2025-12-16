using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;

namespace SIQuester.ViewModel.Tests.Mocks;

/// <summary>
/// Mock implementation of package templates repository for testing.
/// </summary>
internal sealed class PackageTemplatesRepositoryMock : IPackageTemplatesRepository
{
    public ICollection<PackageTemplate> Templates { get; } = new List<PackageTemplate>();

    public void AddTemplate(PackageTemplate packageTemplate)
    {
        Templates.Add(packageTemplate);
    }

    public void RemoveTemplate(PackageTemplate packageTemplate)
    {
        Templates.Remove(packageTemplate);
    }

    public Task<SIDocument> GetTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        // Return a simple empty package
        var document = SIDocument.Create(name, "Test Author");
        return Task.FromResult(document);
    }
}
