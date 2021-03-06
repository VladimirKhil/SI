using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public abstract class ItemsViewModel<T> : ObservableCollection<T>, IItemsViewModel
    {
        public ICommand AddItem { get; private set; }
        public new SimpleCommand RemoveItem { get; private set; }

        public SimpleCommand MoveLeft { get; private set; }
        public SimpleCommand MoveRight { get; private set; }

        public abstract QDocument OwnerDocument { get; }

        private int _currentPosition;

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    if (_currentPosition > -1 && _currentPosition < Count)
                        CurrentItem = this[_currentPosition];

                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentPosition)));
                }
            }
        }

        private T _currentItem;

        public T CurrentItem
        {
            get => _currentItem;
            set
            {
                if (!Equals(_currentItem, value))
                {
                    var oldValue = _currentItem;
                    _currentItem = value;
                    CurrentPosition = IndexOf(_currentItem);
                    OnCurrentItemChanged(oldValue, value);

                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItem)));
                }
            }
        }

        protected virtual void OnCurrentItemChanged(T oldValue, T newValue) => UpdateCommands();

        public void SetCurrentItem(object item)
        {
            CurrentItem = (T)item;
        }

        protected ItemsViewModel() => Init();

        protected ItemsViewModel(IEnumerable<T> collection)
            : base(collection) => Init();

        private void Init()
        {
            AddItem = new SimpleCommand(AddItem_Executed);
            RemoveItem = new SimpleCommand(RemoveItem_Executed);

            MoveLeft = new SimpleCommand(MoveLeft_Executed);
            MoveRight = new SimpleCommand(MoveRight_Executed);
        }

        public void UpdateCommands()
        {
            var position = _currentPosition;

            MoveLeft.CanBeExecuted = position > 0;
            MoveRight.CanBeExecuted = position > -1 && position + 1 < Count;

            RemoveItem.CanBeExecuted = CanRemove();
        }

        protected virtual bool CanRemove() => true;

        private void AddItem_Executed(object arg) => Add((T)(object)"");

        private void RemoveItem_Executed(object arg)
        {
            if (_currentPosition > -1 && _currentPosition < Count)
            {
                RemoveAt(_currentPosition);
            }
        }

        private void MoveLeft_Executed(object arg)
        {
            var index = _currentPosition;
            if (index < 1 || index >= Count)
            {
                return;
            }

            var document = OwnerDocument;

            if (document == null)
            {
                return;
            }

            document.BeginChange();

            try
            {
                var tmp = this[index];
                this[index] = this[index - 1];
                this[index - 1] = tmp;

                CurrentItem = this[_currentPosition];
                UpdateCommands();
            }
            finally
            {
                document.CommitChange();
            }
        }

        private void MoveRight_Executed(object arg)
        {
            var index = _currentPosition;

            var document = OwnerDocument;

            if (document == null)
            {
                return;
            }

            document.BeginChange();

            try
            {
                var tmp = this[index];
                this[index] = this[index + 1];
                this[index + 1] = tmp;

                CurrentItem = this[_currentPosition];
                UpdateCommands();

                document.CommitChange();
            }
            catch (Exception exc)
            {
                document.RollbackChange();
                PlatformSpecific.PlatformManager.Instance.ShowExclamationMessage(exc.ToString());
            }
        }
    }
}
