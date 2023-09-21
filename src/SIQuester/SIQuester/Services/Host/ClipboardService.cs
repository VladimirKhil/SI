using SIQuester.ViewModel.Contracts.Host;
using System.Windows;

namespace SIQuester.Services.Host;

internal sealed class ClipboardService : IClipboardService
{
    public object GetData(string format) => Clipboard.GetData(format);

    public void SetData(string format, object data) => Clipboard.SetData(format, data);
}
