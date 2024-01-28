using SImulator.Properties;
using SImulator.ViewModel.Contracts;

namespace SImulator.Implementation;

/// <inheritdoc cref="IDisplayDescriptor" />
internal sealed class WebDisplayDescriptor : IDisplayDescriptor
{
    internal static readonly WebDisplayDescriptor Instance = new();

    public string Name => Resources.WebView;

    public bool IsFullScreen => false;

    public bool IsWebView => true;
}
