namespace SIQuester.ViewModel;

internal sealed class CustomChange : IChange
{
    private readonly Action _undo;
    private readonly Action _redo;

    public void Undo() => _undo();

    public void Redo() => _redo();

    public CustomChange(Action undo, Action redo)
    {
        _undo = undo;
        _redo = redo;
    }
}
