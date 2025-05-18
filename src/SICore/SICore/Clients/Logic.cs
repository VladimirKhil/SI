namespace SICore;

// TODO: remove

/// <summary>
/// Represents agent logic.
/// </summary>
/// <typeparam name="D">Agent data type.</typeparam>
public abstract class Logic<D>
    where D : Data
{
    /// <summary>
    /// Agent data.
    /// </summary>
    protected D _data; // TODO: field must be private. Implement a property for protected access.

    /// <summary>
    /// Typed agent data.
    /// </summary>
    public D ClientData => _data;

    public Logic(D data) => _data = data;
}
