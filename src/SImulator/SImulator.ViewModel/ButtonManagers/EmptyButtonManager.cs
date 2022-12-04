namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Provides a no-op button manager (useful for external buttons).
/// </summary>
internal sealed class EmptyButtonManager : ButtonManagerBase
{
    public override bool Start() => true;

    public override void Stop() { }
}
