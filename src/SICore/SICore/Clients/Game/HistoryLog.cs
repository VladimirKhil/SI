namespace SICore.Clients.Game;

internal sealed class HistoryLog
{
    internal const int MaxSize = 100;

    private readonly Queue<string> _history = new(MaxSize);

    internal void AddLogEntry(string message)
    {
        if (_history.Count > MaxSize)
        {
            _history.Dequeue();
        }

        _history.Enqueue(message);
    }

    public override string ToString() => string.Join(", ", _history);
}
