namespace SIEngine;

/// <summary>
/// Provides methods to manage round questions table.
/// </summary>
public interface IRoundTableController
{
    /// <summary>
    /// Tries to remove a question from table.
    /// </summary>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    /// <returns>Remove succccess status.</returns>
    bool RemoveQuestion(int themeIndex, int questionIndex);

    /// <summary>
    /// Tries to restore question on table.
    /// </summary>
    /// <param name="themeIndex">Question theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    /// <returns>Restored question price.</returns>
    bool RestoreQuestion(int themeIndex, int questionIndex);
}
