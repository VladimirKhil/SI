using SIQuester.Model;
using SIQuester.ViewModel.Model;

namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Defines package templates repository.
/// </summary>
public interface IPackageTemplatesRepository
{
    internal static readonly string TemplateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppSettings.TemplatesFolderName);

    /// <summary>
    /// Current templates.
    /// </summary>
    ICollection<PackageTemplate> Templates { get; }

    /// <summary>
    /// Adds new template to repository.
    /// </summary>
    /// <param name="packageTemplate">New package template.</param>
    void AddTemplate(PackageTemplate packageTemplate);

    /// <summary>
    /// Removes template from repository.
    /// </summary>
    /// <param name="packageTemplate">Template to remove.</param>
    void RemoveTemplate(PackageTemplate packageTemplate);
}
