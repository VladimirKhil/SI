using SIQuester.ViewModel.Contracts.Host;

namespace SIQuester.ViewModel.Tests.Mocks;

/// <summary>
/// Mock implementation of clipboard service for testing.
/// </summary>
internal sealed class ClipboardServiceMock : IClipboardService
{
    private readonly Dictionary<string, object> _data = new();

    public bool ContainsData(string format) => _data.ContainsKey(format);

    public object GetData(string format) => _data.TryGetValue(format, out var data) ? data : null!;

    public void SetData(string format, object data) => _data[format] = data;

    public void Clear() => _data.Clear();
}
