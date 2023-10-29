using SIPackages;

namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Provides method for creating documents.
/// </summary>
public interface IDocumentViewModelFactory
{
    /// <summary>
    /// Creates view model for the document.
    /// </summary>
    /// <param name="document">Package document.</param>
    /// <param name="fileName">Package file name.</param>
    QDocument CreateViewModelFor(SIDocument document, string? fileName = null);
}
