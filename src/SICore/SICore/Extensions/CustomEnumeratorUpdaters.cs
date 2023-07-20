namespace SICore.Extensions;

/// <summary>
/// Provides well-known updaters for custom enumerators.
/// </summary>
public static class CustomEnumeratorUpdaters
{
    /// <summary>
    /// Creates remover by index value function.
    /// </summary>
    /// <param name="index">Index to remove.</param>
    public static Func<int, int?> RemoveByIndex(int index) => (value) =>
    {
        if (value < index)
        {
            return value;
        }

        if (value > index)
        {
            return value - 1;
        }

        return null;
    };
}
