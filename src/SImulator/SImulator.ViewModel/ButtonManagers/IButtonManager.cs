namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Supports players buttons.
/// </summary>
public interface IButtonManager : IAsyncDisposable
{
    /// <summary>
    /// Enables players buttons.
    /// </summary>
    /// <returns>Has the start been successfull.</returns>
    bool Start();

    /// <summary>
    /// Disables players buttons.
    /// </summary>
    void Stop();
}
