namespace SImulator.ViewModel.PlatformSpecific;

public interface ILogger : IDisposable
{
    void Write(string message, params object?[] args);
}
