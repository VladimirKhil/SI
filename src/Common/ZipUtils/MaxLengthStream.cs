namespace ZipUtils;

/// <summary>
/// Defines a stream with limited maximum length.
/// </summary>
/// <see href="https://www.meziantou.net/prevent-zip-bombs-in-dotnet.htm" />
internal sealed class MaxLengthStream : Stream
{
    private readonly Stream _stream;

    private long _length = 0L;

    public long MaxLength { get; }

    public MaxLengthStream(Stream stream, long maxLength)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        _stream = stream;
        MaxLength = maxLength;
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var result = _stream.Read(buffer, offset, count);
        _length += result;

        if (_length > MaxLength)
        {
            throw new InvalidOperationException("Stream is larger than the maximum allowed size");
        }

        return result;
    }

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
        base.Dispose(disposing);
    }
}
