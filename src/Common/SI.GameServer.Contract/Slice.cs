namespace SI.GameServer.Contract;

/// <summary>
/// Defines an array slice (a continious part of an array).
/// It is used for sending large and dynamically changing arrays over the network.
/// </summary>
/// <typeparam name="T">Array item type.</typeparam>
public sealed class Slice<T>
{
    /// <summary>
    /// Empty slice.
    /// </summary>
    public static readonly Slice<GameInfo> Empty = new()
    {
        IsLastSlice = true,
    };

    /// <summary>
    /// Slice data.
    /// </summary>
    public T[] Data { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Is it the last slice in the sequence of slices.
    /// </summary>
    public bool IsLastSlice { get; set; }
}
