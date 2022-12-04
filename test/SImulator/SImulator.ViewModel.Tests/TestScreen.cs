using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Tests;

internal sealed class TestScreen : IScreen
{
    public string Name => throw new NotImplementedException();

    public bool IsRemote => false;
}
