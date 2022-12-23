using SIQuester.ViewModel.Contracts;
using System.Text;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// Provides text from file.
/// </summary>
internal sealed class FileTextSource : ITextSource
{
    private readonly string _filePath;

    public string? FileName => Path.GetFileName(_filePath);

    public FileTextSource(string filePath) => _filePath = filePath;

    public string GetText(Encoding encoding) => File.ReadAllText(_filePath, encoding);

    public void Dispose() { }
}
