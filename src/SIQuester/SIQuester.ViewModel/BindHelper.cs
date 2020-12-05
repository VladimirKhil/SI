using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIQuester.ViewModel
{
    internal static class BindHelper
    {
        internal static void Bind<T>(ObservableCollection<T> collection, IList<T> target)
        {
            collection.CollectionChanged += (sender, e) =>
                {
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            for (int i = 0; i < e.NewItems.Count; i++)
                            {
                                target.Insert(e.NewStartingIndex + i, (T)e.NewItems[i]);
                            }
                            break;

                        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            target[e.NewStartingIndex] = collection[e.NewStartingIndex];
                            target[e.OldStartingIndex] = collection[e.OldStartingIndex];
                            break;

                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            for (int i = 0; i < e.OldItems.Count; i++)
                            {
                                target.RemoveAt(e.OldStartingIndex);
                            }
                            break;

                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            for (int i = 0; i < e.NewItems.Count; i++)
                            {
                                target[e.NewStartingIndex + i] = (T)e.NewItems[i];
                            }
                            break;

                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            target.Clear();
                            foreach (var item in collection)
                            {
                                target.Add(item);
                            }
                            break;
                    }
                };
        }
    }
}
