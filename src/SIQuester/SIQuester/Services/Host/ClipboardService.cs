using SIQuester.ViewModel.Contracts.Host;
using System.Windows;

namespace SIQuester.Services.Host;

/// <inheritdoc />
internal sealed class ClipboardService : IClipboardService
{
    public bool ContainsData(string format) => Clipboard.ContainsData(format);

    public object GetData(string format) => Clipboard.GetData(format);

    public void SetData(string format, object data) => Clipboard.SetData(format, data);
}
