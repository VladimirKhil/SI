using SI.GameServer.Client;

namespace SIGame.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of IUIThreadExecutor.
/// </summary>
internal sealed class TestUIThreadExecutor : IUIThreadExecutor
{
    public void ExecuteOnUIThread(Action action) => action();
}
