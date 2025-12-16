using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.Tests;

/// <summary>
/// Test implementation of IDisplayDescriptor that simulates a web view screen.
/// </summary>
internal sealed class TestWebScreen : IDisplayDescriptor
{
    public bool IsWebView => true;

    public string Name => "Test Web Screen";

    public bool IsFullScreen => false;
}
