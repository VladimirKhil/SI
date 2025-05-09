using SIPackages;

namespace SIEngine;

// TODO: remove this class

public abstract class EngineBase : IDisposable
{
    private bool _isDisposed = false;

    protected readonly SIDocument _document;

    // TODO: hide
    public SIDocument Document => _document;

    protected EngineBase(SIDocument document)
    {
        _document = document;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _document.Dispose();

        _isDisposed = true;
    }
}
