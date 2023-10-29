namespace SIQuester.ViewModel.Contracts.Host;

/// <summary>
/// Provides methids for interacting with system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Queries the Clipboard for the presence of data in a specified data format.
    /// </summary>
    /// <param name="format">Data format.</param>
    bool ContainsData(string format);

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
