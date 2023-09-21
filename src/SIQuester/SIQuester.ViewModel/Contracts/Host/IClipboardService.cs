namespace SIQuester.ViewModel.Contracts.Host;

/// <summary>
/// Provides methids for interacting with system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Retrieves data in a specified format from the Clipboard.
    /// </summary>
    /// <param name="format">Data format.</param>
    object GetData(string format);

    /// <summary>
    /// Stores the specified data on the Clipboard in the specified format.
    /// </summary>
    /// <param name="format">Data format.</param>
    /// <param name="data">Stored data.</param>
    void SetData(string format, object data);
}
