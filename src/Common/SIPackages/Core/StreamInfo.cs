namespace SIPackages.Core;

/// <summary>
/// Wraps the stream adding <see cref="Length" /> property to it.
/// </summary>
/// <remarks>Not all stream types support Length property by default.</remarks>
/// <param name="Stream">Wrapped stream..</param>
/// <param name="Length">Stream length.</param>
public sealed record StreamInfo(Stream Stream, long Length);
