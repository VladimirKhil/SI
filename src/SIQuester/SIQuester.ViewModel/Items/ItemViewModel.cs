using SIPackages;
using SIQuester.Model;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Элемент пакета
    /// </summary>
    public abstract class ItemViewModel<T> : ModelViewBase, IItemViewModel
        where T: InfoOwner
    {
        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public T Model { get; }

        public InfoViewModel Info { get; private set; }

        public SimpleCommand AddAuthors { get; private set; }
        public SimpleCommand AddSources { get; private set; }
        public SimpleCommand AddComments { get; private set; }

        public ICommand SetCosts { get; private set; }

        public InfoOwner GetModel() => Model;

        protected ItemViewModel(T model)
        {
            Model = model;
            Info = new InfoViewModel(Model.Info, this);

            AddAuthors = new SimpleCommand(AddAuthors_Executed) { CanBeExecuted = Info.Authors.Count == 0 };
            AddSources = new SimpleCommand(AddSources_Executed) { CanBeExecuted = Info.Sources.Count == 0 };
            AddComments = new SimpleCommand(AddComments_Executed) { CanBeExecuted = Info.Comments.Text.Length == 0 };

            SetCosts = new SimpleCommand(SetCosts_Executed);

            Info.Authors.CollectionChanged += Authors_CollectionChanged;
            Info.Sources.CollectionChanged += Sources_CollectionChanged;
            Info.Comments.PropertyChanged += Comments_PropertyChanged;
        }

        private void Comments_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AddComments.CanBeExecuted = Info.Comments.Text.Length == 0;
        }

        private void Sources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AddSources.CanBeExecuted = Info.Sources.Count == 0;
        }

        private void Authors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AddAuthors.CanBeExecuted = Info.Authors.Count == 0;
        }

        public abstract IItemViewModel Owner { get; }

        public abstract ICommand Add { get; protected set; }
        public abstract ICommand Remove { get; protected set; }

        private void AddAuthors_Executed(object arg)
        {
            QDocument.ActivatedObject = Info.Authors;
            Info.Authors.Add("");
        }

        private void AddSources_Executed(object arg)
        {
            QDocument.ActivatedObject = Info.Sources;
            Info.Sources.Add("");
        }

        private void AddComments_Executed(object arg)
        {
            QDocument.ActivatedObject = Info.Comments;
            Info.Comments.Text = "Комментарий";
        }

        private void SetCosts_Executed(object arg)
        {
            UpdateCosts((CostSetter)arg);
        }

        protected virtual void UpdateCosts(CostSetter costSetter)
        {
            
        }
    }
}
