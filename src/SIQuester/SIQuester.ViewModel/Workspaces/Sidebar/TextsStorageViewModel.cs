using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Input;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;
using SIQuester.Model;
using SIPackages;
using System.Collections.ObjectModel;
using SIPackages.Core;

namespace SIQuester.ViewModel
{
    public abstract class TextsStorageViewModelBase : WorkspaceViewModel 
    {
        internal event Action<IChange> Changed;

        protected internal void OnChanged(IChange change)
        {
            Changed?.Invoke(change);
        }
    }

    public abstract class TextsStorageViewModel<T> : TextsStorageViewModelBase
        where T: INotifyPropertyChanged, new()
    {
        private readonly string _title;

        private readonly IList<T> _sourceList;

        public override string Header => _title;

        public ObservableCollection<T> Collection { get; private set; }

        private int _currentIndex;

        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                if (!Equals(_currentIndex, value))
                {
                    _currentIndex = value;
                    CheckCommands();
                }
            }
        }
        
        public SimpleCommand MoveUp { get; private set; }
        public SimpleCommand MoveDown { get; private set; }
        public ICommand Add { get; private set; }
        public SimpleCommand Remove { get; private set; }       

        protected TextsStorageViewModel(string title, IList<T> sourceList)
        {
            _title = title;
            _sourceList = sourceList;

            Collection = new ObservableCollection<T>(_sourceList);
            Collection.CollectionChanged += Collection_CollectionChanged;

            BindHelper.Bind<T>(Collection, _sourceList);

            foreach (var item in Collection)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            MoveUp = new SimpleCommand(MoveUp_Executed);
            MoveDown = new SimpleCommand(MoveDown_Executed);
            Add = new SimpleCommand(Add_Executed);
            Remove = new SimpleCommand(Remove_Executed);

            CheckCommands();
        }

        void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnChanged(new CollectionChange { Collection = (IList)sender, Args = e });
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Year") // TODO -> nameof
            {
                var ext = e as ExtendedPropertyChangedEventArgs<int>;
                OnChanged(new SimplePropertyValueChange { Element = sender, PropertyName = e.PropertyName, Value = ext.OldValue });
            }
            else
            {
                var ext = e as ExtendedPropertyChangedEventArgs<string>;
                OnChanged(new SimplePropertyValueChange { Element = sender, PropertyName = e.PropertyName, Value = ext.OldValue });
            }
        }

        private void CheckCommands()
        {
            MoveUp.CanBeExecuted = CurrentIndex > 0;
            MoveDown.CanBeExecuted = CurrentIndex > -1 && CurrentIndex + 1 < Collection.Count;
        }

        private void MoveUp_Executed(object arg)
        {
            var index = CurrentIndex;
            Collection.Move(index, index - 1);
            CheckCommands();
        }

        private void MoveDown_Executed(object arg)
        {
            var index = CurrentIndex;
            Collection.Move(index, index + 1);
            CheckCommands();
        }

        private void Add_Executed(object arg)
        {
            var item = new T();
            item.PropertyChanged += Item_PropertyChanged;

            Collection.Add(item);
        }

        private void Remove_Executed(object arg)
        {
            var item = (T)arg;
            item.PropertyChanged -= Item_PropertyChanged;

            Collection.Remove(item);
        }
    }

    public sealed class AuthorsStorageViewModel : TextsStorageViewModel<AuthorInfo> 
    {
        public AuthorsStorageViewModel(QDocument document)
            : base("Авторы", document.Document.Authors)
        {

        }
    }

    public sealed class SourcesStorageViewModel : TextsStorageViewModel<SourceInfo>
    {
        public SourcesStorageViewModel(QDocument document)
            : base("Источники", document.Document.Sources)
        {

        }
    }
}
