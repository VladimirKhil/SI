namespace SI.GameServer.Contract;

public sealed class Slice<T>
{
    public T[] Data { get; set; }

    public bool IsLastSlice { get; set; }
}
