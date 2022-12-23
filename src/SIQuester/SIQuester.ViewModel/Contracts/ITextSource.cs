using System.Text;

namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Allows to get text using provided encoding.
/// </summary>
internal interface ITextSource : IDisposable
{
    /// <summary>
    /// Optional source file name.
    /// </summary>
    string? FileName => null;

    /// <summary>
    /// Gets source text.
    /// </summary>
    /// <param name="encoding">Text encoding to use.</param>
    string GetText(Encoding encoding);
}
