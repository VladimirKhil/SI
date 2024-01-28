using SImulator.Properties;
using SImulator.ViewModel.Contracts;

namespace SImulator.Implementation;

/// <inheritdoc cref="IDisplayDescriptor" />
internal sealed class WindowDisplayDescriptor : IDisplayDescriptor
{
    internal static readonly WindowDisplayDescriptor Instance = new();

    public string Name => Resources.Window;

    public bool IsFullScreen => false;
}
