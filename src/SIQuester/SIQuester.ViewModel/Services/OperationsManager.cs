using SIQuester.ViewModel.Properties;
using Utils.Commands;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// Holds a queue of operations. Supports undo-ing and redo-ing of them.
/// </summary>
public sealed class OperationsManager
{
    private const int MaxUndoListCount = 100;

    // TODO: rewrite undo-redo to a single linked list of changes and a pointer pointing to the current state in this list
    private readonly Stack<IChange> _undoList = new();
    private readonly Stack<IChange> _redoList = new();

    internal bool IsMakingUndo { get; private set; } = false; // Blocks undo-operations for Undo itself

    private ChangeGroup? _changeGroup = null;

    /// <summary>
    /// Undo command.
    /// </summary>
    public SimpleCommand Undo { get; private set; }

    /// <summary>
    /// Redo command.
    /// </summary>
    public SimpleCommand Redo { get; private set; }

    public event Action<Exception>? Error;

    public event Action? Changed;

    public OperationsManager()
    {
        Undo = new SimpleCommand(Undo_Executed) { CanBeExecuted = false };
        Redo = new SimpleCommand(Redo_Executed) { CanBeExecuted = false };
    }

    public void CanUndoChanged() => Undo.CanBeExecuted = _undoList.Any();

    public void CanRedoChanged() => Redo.CanBeExecuted = _redoList.Any();

    internal void AddChange(IChange change)
    {
        if (IsMakingUndo)
        {
            return;
        }

        if (_changeGroup != null)
        {
            _changeGroup.Add(change);
        }
        else
        {
            if (_undoList.Any())
            {
                if (_undoList.Peek().Equals(change))
                {
                    return;
                }

                if (_undoList.Count == MaxUndoListCount * 2)
                {
                    var last = new Stack<IChange>(_undoList.Take(MaxUndoListCount));
                    _undoList.Clear();

                    foreach (var item in last)
                    {
                        _undoList.Push(item);
                    }
                }
            }

            AddUndo(change);
            ClearRedo();
            OnChanged();
        }
    }

    private void Undo_Executed(object? arg)
    {
        IsMakingUndo = true;

        try
        {
            var item = _undoList.Pop();
            CanUndoChanged();

            item.Undo();

            _redoList.Push(item);
            CanRedoChanged();
            OnChanged();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            IsMakingUndo = false;
        }
    }

    private void Redo_Executed(object? arg)
    {
        IsMakingUndo = true;

        try
        {
            var item = _redoList.Pop();
            CanRedoChanged();

            item.Redo();

            AddUndo(item);
            OnChanged();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            IsMakingUndo = false;
        }
    }

    private void OnChanged() => Changed?.Invoke();

    private void OnError(Exception exc) => Error?.Invoke(exc);

    private void ClearRedo()
    {
        _redoList.Clear();
        CanRedoChanged();
    }

    private void AddUndo(IChange change)
    {
        _undoList.Push(change);
        CanUndoChanged();
    }


    /// <summary>
    /// Starts complex group of changes that should be reverted as a single change.
    /// </summary>
    public ComplexChangeManager BeginComplexChange()
    {
        if (_changeGroup != null)
        {
            throw new Exception(Resources.ChangeGroupIsActivated);
        }

        _changeGroup = new ChangeGroup();

        return new ComplexChangeManager(this);
    }

    /// <summary>
    /// Finishes complex change.
    /// </summary>
    internal void CommitChange()
    {
        if (_changeGroup != null && _changeGroup.Count > 0)
        {
            AddUndo(_changeGroup);
            ClearRedo();
            OnChanged();
        }

        _changeGroup = null;
    }

    /// <summary>
    /// Rollbacks complex change.
    /// </summary>
    internal void RollbackChange()
    {
        if (_changeGroup != null && _changeGroup.Count > 0)
        {
            IsMakingUndo = true;

            try
            {
                _changeGroup.Undo();
            }
            finally
            {
                IsMakingUndo = false;
            }
        }

        _changeGroup = null;
    }
}
