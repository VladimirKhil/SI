namespace SIQuester;

/// <summary>
/// Defines a data change that can be undone or redone.
/// </summary>
public interface IChange
{
    /// <summary>
    /// Undoes the change.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the change.
    /// </summary>
    void Redo();
}
