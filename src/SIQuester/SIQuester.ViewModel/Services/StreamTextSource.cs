using SIQuester.ViewModel.Contracts;
using System.Text;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// Provides text from stream.
/// </summary>
internal sealed class StreamTextSource : ITextSource
{
    private readonly Stream _stream;

	public StreamTextSource(Stream stream) => _stream = stream;

    public string GetText(Encoding encoding)
    {
        using var reader = new StreamReader(_stream, encoding);
        return reader.ReadToEnd();
    }

    public void Dispose() => _stream.Dispose();
}
