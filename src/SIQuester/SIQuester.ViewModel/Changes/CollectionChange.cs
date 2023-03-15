using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.Collections;
using System.Collections.Specialized;

namespace SIQuester;

/// <summary>
/// Defines a collection change.
/// </summary>
public sealed class CollectionChange : IChange
{
    /// <summary>
    /// Collection that has been changed.
    /// </summary>
    public IList Collection { get; init; }

    /// <summary>
    /// Change arguments.
    /// </summary>
    public NotifyCollectionChangedEventArgs Args { get; init; }

    public CollectionChange(IList collection, NotifyCollectionChangedEventArgs args)
    {
        Collection = collection;
        Args = args;
    }

    public void Undo()
    {
        try
        {
            switch (Args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < Args.NewItems.Count; i++)
                    {
                        if (Args.NewStartingIndex > -1 && Args.NewStartingIndex < Collection.Count)
                            Collection.RemoveAt(Args.NewStartingIndex);
                        else
                        {
                            PlatformManager.Instance.ShowErrorMessage(Resources.UndoError);
                            return;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        var index = Args.OldStartingIndex;
                        foreach (var item in Args.OldItems)
                        {
                            Collection.Insert(index++, item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = Args.OldStartingIndex;
                        foreach (var item in Args.OldItems)
                        {
                            Collection[index++] = item;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    // TODO: this seems to be incorrect. Write unit test to check this logic
                    var newIndex = Args.NewStartingIndex - (Args.OldStartingIndex < Args.NewStartingIndex ? 1 : 0);
                    var tmp = Collection[newIndex];
                    Collection.RemoveAt(newIndex);
                    Collection.Insert(Args.OldStartingIndex, tmp);
                    break;
            }
        }
        catch
        {
            PlatformManager.Instance.ShowErrorMessage(Resources.UndoError);
        }
    }

    public void Redo()
    {
        switch (Args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    var index = Args.NewStartingIndex;
                    for (int i = 0; i < Args.NewItems.Count; i++)
                    {
                        Collection.Insert(index++, Args.NewItems[i]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Move:
                var tmp = Collection[Args.OldStartingIndex];
                Collection.RemoveAt(Args.OldStartingIndex);
                Collection.Insert(Args.NewStartingIndex - (Args.OldStartingIndex < Args.NewStartingIndex ? 1 : 0), tmp);
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < Args.OldItems.Count; i++)
                {
                    Collection.RemoveAt(Args.OldStartingIndex);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                {
                    var index = Args.NewStartingIndex;
                    foreach (var item in Args.NewItems)
                    {
                        Collection[index++] = item;
                    }
                }
                break;
        }
    }
}
