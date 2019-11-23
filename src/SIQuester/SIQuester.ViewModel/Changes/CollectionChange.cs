using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using SIQuester.Model;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;

namespace SIQuester
{
    /// <summary>
    /// Изменение коллекции
    /// </summary>
    public sealed class CollectionChange: IChange
    {
        /// <summary>
        /// Коллекция, которая была изменена
        /// </summary>
        public IList Collection { get; set; }
        /// <summary>
        /// Элементы, затронутые изменением
        /// </summary>
        public NotifyCollectionChangedEventArgs Args { get; set; }

        #region IChange Members

        public void Undo()
        {
            try
            {
                switch (this.Args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        for (int i = 0; i < this.Args.NewItems.Count; i++)
                        {
                            if (this.Args.NewStartingIndex > -1 && this.Args.NewStartingIndex < this.Collection.Count)
                                this.Collection.RemoveAt(this.Args.NewStartingIndex);
                            else
                            {
                                PlatformManager.Instance.ShowErrorMessage(Resources.UndoError);
                                return;
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        {
                            var index = this.Args.OldStartingIndex;
                            foreach (var item in this.Args.OldItems)
                            {
                                this.Collection.Insert(index++, item);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        {
                            var index = this.Args.OldStartingIndex;
                            foreach (var item in this.Args.OldItems)
                            {
                                this.Collection[index++] = item;
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Move:
                        var newIndex = this.Args.NewStartingIndex - (this.Args.OldStartingIndex < this.Args.NewStartingIndex ? 1 : 0);
                        var tmp = this.Collection[newIndex];
                        this.Collection.RemoveAt(newIndex);
                        this.Collection.Insert(this.Args.OldStartingIndex, tmp);
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
            switch (this.Args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var index = this.Args.NewStartingIndex;
                        for (int i = 0; i < this.Args.NewItems.Count; i++)
                        {
                            this.Collection.Insert(index++, this.Args.NewItems[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    var tmp = this.Collection[this.Args.OldStartingIndex];
                    this.Collection.RemoveAt(this.Args.OldStartingIndex);
                    this.Collection.Insert(this.Args.NewStartingIndex - (this.Args.OldStartingIndex < this.Args.NewStartingIndex ? 1 : 0), tmp);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < this.Args.OldItems.Count; i++)
                    {
                        this.Collection.RemoveAt(this.Args.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = this.Args.NewStartingIndex;
                        foreach (var item in this.Args.NewItems)
                        {
                            this.Collection[index++] = item;
                        }
                    }
                    break;
            }
        }

        #endregion
    }
}
