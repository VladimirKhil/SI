namespace SICore.Models;

/// <summary>
/// Defines a media content completion that waits for human showman and players to finish watching media.
/// </summary>
internal sealed class Completion
{
    private readonly HashSet<string> _pending;

    /// <summary>
    /// Total awaited number of watchers.
    /// </summary>
    internal int Total { get; }

    /// <summary>
    /// Current number of completions.
    /// </summary>
    internal int Current => Total - _pending.Count;

    /// <summary>
    /// Are all completions received.
    /// </summary>
    internal bool IsComplete => _pending.Count == 0;

    public Completion(IEnumerable<string> persons)
    {
        _pending = [.. persons];
        Total = _pending.Count;
    }

    /// <summary>
    /// Registers person completion. Returns false if the person is unknown or has already completed.
    /// </summary>
    internal bool TryComplete(string person) => _pending.Remove(person);
}
