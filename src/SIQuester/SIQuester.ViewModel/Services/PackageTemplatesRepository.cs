using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using System.Text.Json;

namespace SIQuester.ViewModel.Services;

/// <inheritdoc />
public sealed class PackageTemplatesRepository : IPackageTemplatesRepository
{
    /// <summary>
    /// Templates folder metadata file name.
    /// </summary>
    private const string TemplatesMetadataFileName = "index.json";

    private static readonly string MetadataFile = Path.Combine(IPackageTemplatesRepository.TemplateFolder, TemplatesMetadataFileName);

    private readonly Lazy<List<PackageTemplate>> _templates = new(LoadTemplates);

    public ICollection<PackageTemplate> Templates => _templates.Value;

    private static List<PackageTemplate> LoadTemplates()
    {
        if (!File.Exists(MetadataFile))
        {
            return new List<PackageTemplate>();
        }

        try
        {
            using var metadataStream = File.OpenRead(MetadataFile);
            return JsonSerializer.Deserialize<List<PackageTemplate>>(metadataStream) ?? new List<PackageTemplate>();
        }
        catch
        {
            return new List<PackageTemplate>();
        }
    }

    public void AddTemplate(PackageTemplate packageTemplate)
    {
        _templates.Value.Add(packageTemplate);
        Save();
    }

    public void RemoveTemplate(PackageTemplate packageTemplate)
    {
        if (_templates.Value.Remove(packageTemplate))
        {
            Save();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(IPackageTemplatesRepository.TemplateFolder);
        File.WriteAllText(MetadataFile, JsonSerializer.Serialize(_templates.Value));
    }
}
