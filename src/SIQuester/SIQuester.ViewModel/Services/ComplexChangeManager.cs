namespace SIQuester.ViewModel.Services;

public sealed class ComplexChangeManager : IDisposable
{
    private readonly OperationsManager _operationsManager;

    private bool _commited;

    public ComplexChangeManager(OperationsManager operationsManager) => _operationsManager = operationsManager;

    public void Commit() => _commited = true;

    public void Dispose()
    {
        if (_commited)
        {
            _operationsManager.CommitChange();
        }
        else
        {
            _operationsManager.RollbackChange();
        }
    }
}
