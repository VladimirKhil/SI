namespace SImulator.ViewModel.PlatformSpecific;

public interface IGameLogger : IDisposable
{
    void Write(string message, params object?[] args);
}
