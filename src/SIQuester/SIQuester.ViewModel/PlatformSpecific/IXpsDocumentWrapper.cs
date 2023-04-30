namespace SIQuester.ViewModel.PlatformSpecific;

public interface IXpsDocumentWrapper : IDisposable
{
    object? TryGetDocument();
}
