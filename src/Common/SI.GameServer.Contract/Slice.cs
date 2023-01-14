namespace SI.GameServer.Contract;

public sealed class Slice<T>
{
    public static readonly Slice<GameInfo> Empty = new()
    {
        Data = Array.Empty<GameInfo>(),
        IsLastSlice = true,
    };

    public T[] Data { get; set; }

    public bool IsLastSlice { get; set; }
}
