using Notions;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Commands;
using SIQuester.ViewModel.Core;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Workspaces.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Xsl;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Документ, открытый в редакторе
    /// </summary>
    public sealed class QDocument: WorkspaceViewModel
    {
        /// <summary>
        /// Созданный объект
        /// </summary>
        public static object ActivatedObject { get; set; }

        private const int MaxUndoListCount = 100;

        private bool _changed = false;
        private readonly Stack<IChange> _undoList = new Stack<IChange>();
        private readonly Stack<IChange> _redoList = new Stack<IChange>();
        private bool _isMakingUndo = false; // Блокирует добавление UnDo-операций для самого UnDo
        private ChangeGroup _changeGroup = null;

        private IItemViewModel _activeNode = null;
        private IItemViewModel[] _activeChain = null;

        public MediaStorageViewModel Images { get; private set; }
        public MediaStorageViewModel Audio { get; private set; }
        public MediaStorageViewModel Video { get; private set; }

        private bool _isProgress;

        public bool IsProgress
        {
            get { return _isProgress; }
            set { if (_isProgress != value) { _isProgress = value; OnPropertyChanged(); } }
        }

        private bool _isLocked = false;

        public bool IsLocked
        {
            get { return _isLocked; }
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;

                    SendToGame.CanBeExecuted = !value;
                }
            }
        }

        private object _dialog = null;

        public object Dialog
        {
            get { return _dialog; }
            set
            {
                if (_dialog != value)
                {
                    _dialog = value;
                    if (_dialog is WorkspaceViewModel workspace)
                    {
                        workspace.Closed += Workspace_Closed;
                    }

                    OnPropertyChanged();
                }
            }
        }

        private void Workspace_Closed(WorkspaceViewModel obj)
        {
            Dialog = null;
        }

        private string _searchText;

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    MakeSearch();
                }
            }
        }

        private CancellationTokenSource _cancellation = null;
        private readonly object _searchSync = new object();

        private async void MakeSearch()
        {
            lock (_searchSync)
            {
                if (_cancellation != null)
                {
                    _cancellation.Cancel();
                    _cancellation = null;
                }
            }

            if (string.IsNullOrEmpty(_searchText))
            {
                SearchFailed = false;
                Navigate.Execute(null);
                ClearSearchText.CanBeExecuted = false;
                NextSearchResult.CanBeExecuted = false;
                PreviousSearchResult.CanBeExecuted = false;
                return;
            }

            _cancellation = new CancellationTokenSource();
            ClearSearchText.CanBeExecuted = true;

            var task = Task.Run(() => Search(_searchText, _cancellation.Token), _cancellation.Token);
            try
            {
                await task;
            }
            catch (Exception exc)
            {
                OnError(exc);
                return;
            }

            if (task.IsCanceled)
                return;

            SearchFailed = SearchResults == null || SearchResults.Results.Count == 0;
            if (!_searchFailed)
            {
                NextSearchResult.CanBeExecuted = true;
                PreviousSearchResult.CanBeExecuted = true;
                Navigate.Execute(SearchResults.Results[SearchResults.Index]);
            }
            else
            {
                NextSearchResult.CanBeExecuted = false;
                PreviousSearchResult.CanBeExecuted = false;
                Navigate.Execute(null);
            }
        }

        internal void ClearLinks(RoundViewModel round)
        {
            if (!AppSettings.Default.RemoveLinks || _isMakingUndo)
            {
                return;
            }

            foreach (var theme in round.Themes)
            {
                ClearLinks(theme);
            }
        }

        internal void ClearLinks(ThemeViewModel theme)
        {
            if (!AppSettings.Default.RemoveLinks || _isMakingUndo)
            {
                return;
            }

            foreach (var question in theme.Questions)
            {
                ClearLinks(question);
            }
        }

        internal void ClearLinks(QuestionViewModel question)
        {
            if (!AppSettings.Default.RemoveLinks || _isMakingUndo)
            {
                return;
            }

            foreach (var atom in question.Scenario)
            {
                ClearLinks(atom);
            }
        }

        internal void ClearLinks(AtomViewModel atom)
        {
            if (!AppSettings.Default.RemoveLinks || _isMakingUndo)
            {
                return;
            }

            var atomType = atom.Model.Type;
            var collection = Images;

            switch (atomType)
            {
                case AtomTypes.Image:
                    break;

                case AtomTypes.Audio:
                    collection = Audio;
                    break;

                case AtomTypes.Video:
                    collection = Video;
                    break;

                default:
                    return;
            }

            var link = atom.Model.Text;
            if (!link.StartsWith("@")) // Внешняя ссылка
                return;

            if (!HasLinksTo(link)) // Вызывается уже после удаления объектов из дерева, так что работает корректно
            {
                for (int i = 0; i < collection.Files.Count; i++)
                {
                    if (collection.Files[i].Model.Name == link.Substring(1))
                    {
                        collection.DeleteItem.Execute(collection.Files[i]);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Имеются ли какие-то ссылки в документе на мультимедиа
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private bool HasLinksTo(string link)
        {
            foreach (var round in Document.Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    foreach (var question in theme.Questions)
                    {
                        foreach (var atom in question.Scenario)
                        {
                            if (atom.Text == link)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool _searchFailed = false;

        public bool SearchFailed
        {
            get { return _searchFailed; }
            set { if (_searchFailed != value) { _searchFailed = value; OnPropertyChanged(); } }
        }

        #region Commands

        /// <summary>
        /// Импортировать существующий пакет
        /// </summary>
        public ICommand ImportSiq { get; private set; }

        /// <summary>
        /// Сохранить
        /// </summary>
        public AsyncCommand Save { get; private set; }

        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportHtml { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportPrintHtml { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportFormattedHtml { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportBase { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportMirc { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportTvSI { get; private set; }
        /// <summary>
        /// Экспорт в HTML
        /// </summary>
        public ICommand ExportSns { get; private set; }
        /// <summary>
        /// Экспорт в формат Динабанка
        /// </summary>
        public ICommand ExportDinabank { get; private set; }
        /// <summary>
        /// Экспорт в табличный формат
        /// </summary>
        public ICommand ExportTable { get; private set; }

        /// <summary>
        /// Викифицировать пакет
        /// </summary>
        public ICommand Wikify { get; private set; }

        public ICommand ConvertToCompTvSI { get; private set; }
        public ICommand ConvertToCompTvSISimple { get; private set; }
        public ICommand ConvertToSportSI { get; private set; }
        public ICommand ConvertToMillionaire { get; private set; }

        public ICommand Navigate { get; private set; }

        public ICommand SelectThemes { get; private set; }

        public ICommand ExpandAll { get; private set; }

        public ICommand CollapseAllMedia { get; private set; }
        public ICommand ExpandAllMedia { get; private set; }

        public SimpleCommand SendToGame { get; private set; }

        public ICommand Delete { get; private set; }

        public SimpleCommand Undo { get; private set; }
        public SimpleCommand Redo { get; private set; }

        public SimpleCommand NextSearchResult { get; private set; }
        public SimpleCommand PreviousSearchResult { get; private set; }
        public SimpleCommand ClearSearchText { get; private set; }

        #endregion

        /// <summary>
        /// Полный путь к текущему узлу
        /// </summary>
        public IItemViewModel ActiveNode
        {
            get { return _activeNode; }
            set
            {
                if (_activeNode != value)
                {
                    _activeNode = value;
                    OnPropertyChanged();
                    SetActiveChain();
                }
            }
        }

        private object _activeItem;

        public object ActiveItem
        {
            get { return _activeItem; }
            set
            {
                if (_activeItem != value)
                {
                    _activeItem = value;
                    OnPropertyChanged();
                }
            }
        }
        
        internal DataCollection GetCollection(string name)
        {
            switch (name)
            {
                case SIDocument.ImagesStorageName:
                    return Document.Images;

                case SIDocument.AudioStorageName:
                    return Document.Audio;

                default:
                    return Document.Video;
            }
        }

        /// <summary>
        /// Создать цепочку от корня к текщему элементу
        /// </summary>
        private void SetActiveChain()
        {
            var chain = new List<IItemViewModel>();
            for (var current = _activeNode; current != null; current = current.Owner)
            {
                chain.Insert(0, current);
            }

            ActiveChain = chain.ToArray();
        }

        public IItemViewModel[] ActiveChain
        {
            get { return _activeChain; }
            private set
            {
                _activeChain = value;
                OnPropertyChanged();
            }
        }

        public SIDocument Document { get; private set; } = null;

        /// <summary>
        /// Объект синхронизации, гарантирующий то, что в момент доступа к медиа-файлам пакет не будет меняться или закрываться
        /// </summary>
        public object Sync { get; } = new object();

        public PackageViewModel Package { get; }

        public PackageViewModel[] Packages => new PackageViewModel[] { Package };

        private string _path = null;

        /// <summary>
        /// Путь к файлу пакета
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Header));
                    OnPropertyChanged(nameof(ToolTip));
                }
            }
        }

        public override string ToolTip => _path;

        private bool NeedSave()
        {
            return Changed || string.IsNullOrEmpty(_path);
        }

        private static string EncodePath(string path)
        {
            var result = new StringBuilder();

            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '%')
                    result.Append("%%");
                else if (c == '\\')
                    result.Append("%)");
                else if (c == '/')
                    result.Append("%(");
                else if (c == ':')
                    result.Append("%;");
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        internal static string DecodePath(string path)
        {
            var result = new StringBuilder();

            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '%' && i + 1 < path.Length)
                {
                    var c1 = path[++i];
                    if (c1 == '%')
                        result.Append('%');
                    else if (c1 == ')')
                        result.Append('\\');
                    else if (c1 == '(')
                        result.Append('/');
                    else if (c1 == ';')
                        result.Append(':');
                    else
                        result.Append(c).Append(c1);
                }
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        protected internal override async Task SaveIfNeeded(bool temp)
        {
            if (temp)
            {
                if (_changed && _lastChangedTime > _lastSavedTime && this._path.Length > 0)
                {
                    lock (Sync)
                    {
                        if (IsLocked)
                            return;

                        IsLocked = true;
                    }

                    try
                    {
                        // Автосохранение документа по временном пути
                        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SIQuester");
                        Directory.CreateDirectory(path);

                        var tempName = System.IO.Path.Combine(path, EncodePath(this._path));
                        using (var stream = File.Open(tempName, FileMode.Create, FileAccess.ReadWrite))
                        {


                            using (var tempDoc = Document.SaveAs(stream, !temp))
                            {
                                if (Images.HasPendingChanges)
                                    await Images.ApplyToAsync(tempDoc.Images);

                                if (Audio.HasPendingChanges)
                                    await Audio.ApplyToAsync(tempDoc.Audio);

                                if (Video.HasPendingChanges)
                                    await Video.ApplyToAsync(tempDoc.Video);

                                tempDoc.FinalizeSave();
                            }

                        }

                        _lastSavedTime = DateTime.Now;
                    }
                    finally
                    {
                        SetUnlock();
                    }
                }
            }
            else if (NeedSave())
                await Save.ExecuteAsync(null);
        }

        private string _filename = null;

        public string FileName
        {
            get { return _filename; }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        /// <summary>
        /// Производились ли изменения в документе
        /// </summary>
        public bool Changed
        {
            get
            {
                return _changed;
            }
            set
            {
                if (_changed != value)
                {
                    _changed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Header));
                }

                if (_changed)
                    _lastChangedTime = DateTime.Now;
            }
        }

        private DateTime _lastChangedTime = DateTime.MinValue;
        private DateTime _lastSavedTime = DateTime.MinValue;

        public override string Header => $"{FileName}{(NeedSave() ? "*" : "")}";

        public SearchResults SearchResults { get; private set; } = null;



        private AuthorsStorageViewModel _authors;

        public AuthorsStorageViewModel Authors
        {
            get
            {
                if (_authors == null)
                {
                    _authors = new AuthorsStorageViewModel(this);
                    _authors.Changed += AddChange;
                }

                return _authors;
            }
        }

        private SourcesStorageViewModel _sources;

        public SourcesStorageViewModel Sources
        {
            get
            {
                if (_sources == null)
                {
                    _sources = new SourcesStorageViewModel(this);
                    _sources.Changed += AddChange;
                }

                return _sources;
            }
        }

        private StatisticsViewModel _statistics;

        public StatisticsViewModel Statistics
        {
            get
            {
                if (_statistics == null)
                {
                    _statistics = new StatisticsViewModel(this);
                }

                return _statistics;
            }
        }

        private void CreatePropertyListeners()
        {
            Package.Model.PropertyChanged += Object_PropertyValueChanged;
            Package.Rounds.CollectionChanged += Object_CollectionChanged;
            Listen(Package);
            foreach (var round in Package.Rounds)
            {
                round.Model.PropertyChanged += Object_PropertyValueChanged;
                round.Themes.CollectionChanged += Object_CollectionChanged;
                Listen(round);
                foreach (var theme in round.Themes)
                {
                    theme.Model.PropertyChanged += Object_PropertyValueChanged;
                    theme.Questions.CollectionChanged += Object_CollectionChanged;
                    Listen(theme);
                    foreach (var question in theme.Questions)
                    {
                        question.Model.PropertyChanged += Object_PropertyValueChanged;
                        question.Type.PropertyChanged += Object_PropertyValueChanged;
                        question.Type.Params.CollectionChanged += Object_CollectionChanged;
                        foreach (var param in question.Type.Params)
                        {
                            param.PropertyChanged += Object_PropertyValueChanged;
                        }

                        question.Scenario.CollectionChanged += Object_CollectionChanged;
                        foreach (var atom in question.Model.Scenario)
                        {
                            atom.PropertyChanged += Object_PropertyValueChanged;
                        }

                        question.Right.CollectionChanged += Object_CollectionChanged;
                        question.Wrong.CollectionChanged += Object_CollectionChanged;
                        Listen(question);
                    }
                }
            }

            Images.HasChanged += Images_Commited;
            Audio.HasChanged += Images_Commited;
            Video.HasChanged += Images_Commited;
        }

        private void Object_PropertyValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isMakingUndo)
                return;

            if (e.PropertyName == nameof(Question.Price) || e.PropertyName == nameof(Atom.AtomTime))
            {
                var ext = (ExtendedPropertyChangedEventArgs<int>)e;
                AddChange(new SimplePropertyValueChange() { Element = sender, PropertyName = e.PropertyName, Value = ext.OldValue });
            }
            else
            {
                if (!(e is ExtendedPropertyChangedEventArgs<string> ext))
                    return;

                if (sender is QuestionTypeViewModel questionType)
                {
                    BeginChange();
                    try
                    {
                        AddChange(new SimplePropertyValueChange { Element = sender, PropertyName = e.PropertyName, Value = ext.OldValue });

                        foreach (var param in questionType.Params)
                        {
                            param.PropertyChanged -= Object_PropertyValueChanged;
                        }

                        var typeName = questionType.Model.Name;
                        if (typeName == QuestionTypes.Cat || typeName == QuestionTypes.BagCat || typeName == QuestionTypes.Auction
                            || typeName == QuestionTypes.Simple || typeName == QuestionTypes.Sponsored)
                        {
                            while (questionType.Params.Count > 0) // Очистим по одному, чтобы иметь возможность отката (Undo reset не работает)
                                questionType.Params.RemoveAt(0);
                        }

                        if (typeName == QuestionTypes.Cat || typeName == QuestionTypes.BagCat)
                        {
                            questionType.AddParam(QuestionTypeParams.Cat_Theme, "");
                            questionType.AddParam(QuestionTypeParams.Cat_Cost, "0");
                            if (typeName == QuestionTypes.BagCat)
                            {
                                questionType.AddParam(QuestionTypeParams.BagCat_Self, QuestionTypeParams.BagCat_Self_Value_False);
                                questionType.AddParam(QuestionTypeParams.BagCat_Knows, QuestionTypeParams.BagCat_Knows_Value_After);
                            }
                        }

                        foreach (var param in questionType.Params)
                        {
                            param.PropertyChanged += Object_PropertyValueChanged;
                        }

                        CommitChange();
                    }
                    catch (Exception exc)
                    {
                        RollbackChange();
                        throw exc;
                    }
                }
                else
                {
                    AddChange(new SimplePropertyValueChange() { Element = sender, PropertyName = e.PropertyName, Value = ext.OldValue });
                }
            }
        }

        private void Images_Commited()
        {
            Changed = true;
        }

        private void Listen(IItemViewModel owner)
        {
            owner.Info.Authors.CollectionChanged += Object_CollectionChanged;
            owner.Info.Sources.CollectionChanged += Object_CollectionChanged;
            owner.Info.Comments.PropertyChanged += Object_PropertyValueChanged;
        }

        private void StopListen(IItemViewModel owner)
        {
            owner.Info.Authors.CollectionChanged -= Object_CollectionChanged;
            owner.Info.Sources.CollectionChanged -= Object_CollectionChanged;
            owner.Info.Comments.PropertyChanged -= Object_PropertyValueChanged;
        }

        private void AddChange(IChange change)
        {
            if (_isMakingUndo)
                return;

            if (_changeGroup != null)
            {
                _changeGroup.Add(change);
            }
            else
            {
                if (_undoList.Any())
                {
                    if (_undoList.Peek().Equals(change))
                        return;

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

                _undoList.Push(change);
                CheckUndoCanBeExecuted();
                _redoList.Clear();
                CheckRedoCanBeExecuted();
                Changed = true;
            }
        }

        private void Object_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isMakingUndo)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is IItemViewModel itemViewModel)
                        {
                            Listen(itemViewModel);
                            itemViewModel.GetModel().PropertyChanged += Object_PropertyValueChanged;
                            if (itemViewModel is PackageViewModel package)
                                package.Rounds.CollectionChanged += Object_CollectionChanged;
                            else
                            {
                                if (itemViewModel is RoundViewModel round)
                                    round.Themes.CollectionChanged += Object_CollectionChanged;
                                else
                                {
                                    if (itemViewModel is ThemeViewModel theme)
                                        theme.Questions.CollectionChanged += Object_CollectionChanged;
                                    else
                                    {
                                        var questionViewModel = (QuestionViewModel)itemViewModel;

                                        questionViewModel.Type.PropertyChanged += Object_PropertyValueChanged;
                                        questionViewModel.Type.Params.CollectionChanged += Object_CollectionChanged;
                                        foreach (var param in questionViewModel.Type.Params)
                                        {
                                            param.PropertyChanged += Object_PropertyValueChanged;
                                        }
                                        questionViewModel.Scenario.CollectionChanged += Object_CollectionChanged;
                                        foreach (var atom in questionViewModel.Scenario)
                                        {
                                            atom.PropertyChanged += Object_PropertyValueChanged;
                                        }
                                        questionViewModel.Right.CollectionChanged += Object_CollectionChanged;
                                        questionViewModel.Wrong.CollectionChanged += Object_CollectionChanged;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item is QuestionTypeViewModel type)
                            {
                                type.PropertyChanged += Object_PropertyValueChanged;
                                type.Params.CollectionChanged += Object_CollectionChanged;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is IItemViewModel itemViewModel)
                        {
                            StopListen(itemViewModel);
                            itemViewModel.GetModel().PropertyChanged -= Object_PropertyValueChanged;
                            if (itemViewModel is PackageViewModel package)
                                package.Rounds.CollectionChanged -= Object_CollectionChanged;
                            else
                            {
                                if (itemViewModel is RoundViewModel round)
                                    round.Themes.CollectionChanged -= Object_CollectionChanged;
                                else
                                {
                                    if (itemViewModel is ThemeViewModel theme)
                                        theme.Questions.CollectionChanged -= Object_CollectionChanged;
                                    else
                                    {
                                        var questionViewModel = (QuestionViewModel)itemViewModel;

                                        questionViewModel.Type.PropertyChanged -= Object_PropertyValueChanged;
                                        questionViewModel.Type.Params.CollectionChanged -= Object_CollectionChanged;
                                        foreach (var param in questionViewModel.Type.Params)
                                        {
                                            param.PropertyChanged -= Object_PropertyValueChanged;
                                        }

                                        questionViewModel.Scenario.CollectionChanged -= Object_CollectionChanged;
                                        foreach (var atom in questionViewModel.Scenario)
                                        {
                                            atom.PropertyChanged -= Object_PropertyValueChanged;
                                        }

                                        questionViewModel.Right.CollectionChanged -= Object_CollectionChanged;
                                        questionViewModel.Wrong.CollectionChanged -= Object_CollectionChanged;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item is QuestionTypeViewModel type)
                            {
                                type.PropertyChanged -= Object_PropertyValueChanged;
                                type.Params.CollectionChanged -= Object_CollectionChanged;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems.Count == e.OldItems.Count)
                    {
                        bool equals = true;
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            if (!e.NewItems[i].Equals(e.OldItems[i]))
                            {
                                equals = false;
                                break;
                            }
                        }
                        if (equals)
                            return;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    return;
            }

            AddChange(new CollectionChange { Collection = (IList)sender, Args = e });            
        }

        public StorageContextViewModel StorageContext { get; set; }

        internal QDocument(SIDocument document, StorageContextViewModel storageContextViewModel)
        {
            StorageContext = storageContextViewModel;

            ImportSiq = new SimpleCommand(ImportSiq_Executed);
            
            Save = new AsyncCommand(Save_Executed);

            ExportHtml = new SimpleCommand(ExportHtml_Executed);
            ExportPrintHtml = new SimpleCommand(ExportPrintHtml_Executed);
            ExportFormattedHtml = new SimpleCommand(ExportFormattedHtml_Executed);
            ExportBase = new SimpleCommand(ExportBase_Executed);
            ExportMirc = new SimpleCommand(ExportMirc_Executed);
            ExportTvSI = new SimpleCommand(ExportTvSI_Executed);
            ExportSns = new SimpleCommand(ExportSns_Executed);
            ExportDinabank = new SimpleCommand(ExportDinabank_Executed);
            ExportTable = new SimpleCommand(ExportTable_Executed);

            ConvertToCompTvSI = new SimpleCommand(ConvertToCompTvSI_Executed);
            ConvertToCompTvSISimple = new SimpleCommand(ConvertToCompTvSISimple_Executed);
            ConvertToMillionaire = new SimpleCommand(ConvertToMillionaire_Executed);
            ConvertToSportSI = new SimpleCommand(ConvertToSportSI_Executed);

            Wikify = new SimpleCommand(Wikify_Executed);          

            Navigate = new SimpleCommand(Navigate_Executed);

            SelectThemes = new SimpleCommand(SelectThemes_Executed);

            ExpandAll = new SimpleCommand(ExpandAll_Executed);

            CollapseAllMedia = new SimpleCommand(CollapseAllMedia_Executed);
            ExpandAllMedia = new SimpleCommand(ExpandAllMedia_Executed);

            SendToGame = new SimpleCommand(SendToGame_Executed);

            Delete = new SimpleCommand(Delete_Executed);

            Undo = new SimpleCommand(Undo_Executed) { CanBeExecuted = false };
            Redo = new SimpleCommand(Redo_Executed) { CanBeExecuted = false };

            NextSearchResult = new SimpleCommand(NextSearchResult_Executed) { CanBeExecuted = false };
            PreviousSearchResult = new SimpleCommand(PreviousSearchResult_Executed) { CanBeExecuted = false };
            ClearSearchText = new SimpleCommand(ClearSearchText_Executed) { CanBeExecuted = false };

            AddCommandBinding(ApplicationCommands.SaveAs, SaveAs_Executed);

            AddCommandBinding(ApplicationCommands.Cut, Cut_Executed);
            AddCommandBinding(ApplicationCommands.Copy, Copy_Executed);
            AddCommandBinding(ApplicationCommands.Paste, Paste_Executed);

            FileName = "";
            Path = "";

            Document = document;
            Package = new PackageViewModel(Document.Package, this);
            Package.Info.Authors.UpdateCommands();

            Package.IsExpanded = true;
            Package.IsSelected = true;
            ActiveNode = Package;
            foreach (var round in Package.Rounds)
            {
                round.IsExpanded = true;
            }

            Images = new MediaStorageViewModel(this, Document.Images, "Изображения");
            Audio = new MediaStorageViewModel(this, Document.Audio, "Аудио");
            Video = new MediaStorageViewModel(this, Document.Video, "Видео");

            Images.Changed += AddChange;
            Audio.Changed += AddChange;
            Video.Changed += AddChange;

            Images.Error += OnError;
            Audio.Error += OnError;
            Video.Error += OnError;

            CreatePropertyListeners();
        }

        private async void SendToGame_Executed(object arg)
        {
            try
            {
                await SaveIfNeeded(false);

                var checkResult = Validate();
                if (!string.IsNullOrWhiteSpace(checkResult))
                {
                    PlatformManager.Instance.Inform(checkResult, true);
                    return;
                }

                Dialog = new SendToGameDialogViewModel(this);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void SetUnlock()
        {
            lock (Sync)
            {
                IsLocked = false;
            }

            Dialog = null;
        }

        private async Task<bool> SetLock()
        {
            var retry = false;
            lock (Sync)
            {
                if (IsLocked)
                    retry = true;
                else
                    IsLocked = true;
            }

            if (retry)
            {
                await Task.Delay(5000);
                lock (Sync)
                {
                    if (IsLocked)
                    {
                        PlatformManager.Instance.ShowExclamationMessage("Не удалось выполнить операцию! Пожалуйста, попробуйте ещё раз.");
                        return false;
                    }
                    else
                        IsLocked = true;
                }
            }

            Dialog = new WaitDialogViewModel();
            return true;
        }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Package.Model.ID))
                Package.Model.ID = Guid.NewGuid().ToString();

            return CheckLinks();
        }

        private void FillFiles(List<string> files, MediaStorageViewModel mediaStorage, List<string> errors)
        {
            foreach (var item in mediaStorage.Files)
            {
                var name = item.Model.Name;
                if (files.Contains(name))
                    errors.Add($"Файл \"{name}\" содержится в пакете дважды!");

                files.Add(name);
            }
        }

        /// <summary>
        /// Проверка битых ссылок и лишних файлов
        /// </summary>
        /// <returns></returns>
        internal string CheckLinks(bool allowExternal = false)
        {
            var images = new List<string>();
            var audio = new List<string>();
            var video = new List<string>();

            var errors = new List<string>();
            var usedFiles = new HashSet<string>();

            FillFiles(images, Images, errors);
            FillFiles(audio, Audio, errors);
            FillFiles(video, Video, errors);

            var crossList = images.Intersect(audio).Union(images.Intersect(video)).Union(audio.Intersect(video)).ToArray();
            if (crossList.Length > 0)
            {
                foreach (var item in crossList)
                {
                    errors.Add($"Файл \"{item}\" содержится в пакете в разных категориях!");
                }
            }

            var logo = Package.Logo;
            if (logo != null)
            {
                if (images.Contains(logo.Uri))
                    usedFiles.Add(logo.Uri);
                else
                    errors.Add($"Логотип пакета: отсутствует файл \"{logo.Uri}\"!");
            }

            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    foreach (var question in theme.Questions)
                    {
                        foreach (var atom in question.Scenario)
                        {
                            List<string> collection = null;
                            switch (atom.Model.Type)
                            {
                                case AtomTypes.Image:
                                    collection = images;
                                    break;

                                case AtomTypes.Audio:
                                    collection = audio;
                                    break;

                                case AtomTypes.Video:
                                    collection = video;
                                    break;
                            }

                            if (collection != null)
                            {
                                var media = Document.GetLink(atom.Model);
                                if (collection.Contains(media.Uri))
                                    usedFiles.Add(media.Uri);
                                else if (allowExternal && !atom.Model.Text.StartsWith("@"))
                                    continue;
                                else
                                    errors.Add($"{round.Model.Name}/{theme.Model.Name}/{question.Model.Price}: отсутствует файл \"{media.Uri}\"! {(allowExternal ? "" : "Внешние ссылки не допускаются")}");
                            }
                        }
                    }
                }
            }

            var extraFiles = images.Union(audio).Union(video).Except(usedFiles).ToArray();
            if (extraFiles.Length > 0)
            {
                foreach (var item in extraFiles)
                {
                    errors.Add($"Файл \"{item}\" не используется. Удалите его.");
                }
            }

            return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Пока не поддерживается
        }

        private const string ClipboardKey = "siqdata";

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_activeNode == null)
                return;

            try
            {
                var itemData = new InfoOwnerData(_activeNode.GetModel());
                itemData.GetFullData(Document, _activeNode.GetModel());

                Clipboard.SetData(ClipboardKey, itemData);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private async void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_activeNode == null)
                return;

            if (!Clipboard.ContainsData(ClipboardKey))
                return;

            try
            {
                var itemData = (InfoOwnerData)Clipboard.GetData(ClipboardKey);
                var level = itemData.ItemLevel;

                if (level == InfoOwnerData.Level.Round)
                {
                    var round = (Round)itemData.GetItem();
                    if (_activeNode is PackageViewModel myPackage)
                    {
                        myPackage.Rounds.Add(new RoundViewModel(round));
                    }
                    else
                    {
                        if (_activeNode is RoundViewModel myRound)
                        {
                            myRound.OwnerPackage.Rounds.Insert(myRound.OwnerPackage.Rounds.IndexOf(myRound), new RoundViewModel(round));
                        }
                        else
                            return;
                    }
                }
                else if (level == InfoOwnerData.Level.Theme)
                {
                    var theme = (Theme)itemData.GetItem();
                    if (_activeNode is RoundViewModel myRound)
                    {
                        myRound.Themes.Add(new ThemeViewModel(theme));
                    }
                    else
                    {
                        if (_activeNode is ThemeViewModel myTheme)
                        {
                            myTheme.OwnerRound.Themes.Insert(myTheme.OwnerRound.Themes.IndexOf(myTheme), new ThemeViewModel(theme));
                        }
                        else
                            return;
                    }
                }
                else if (level == InfoOwnerData.Level.Question)
                {
                    var question = (Question)itemData.GetItem();
                    if (_activeNode is ThemeViewModel myTheme)
                    {
                        myTheme.Questions.Add(new QuestionViewModel(question));
                    }
                    else
                    {
                        if (_activeNode is QuestionViewModel myQuestion)
                        {
                            myQuestion.OwnerTheme.Questions.Insert(myQuestion.OwnerTheme.Questions.IndexOf(myQuestion), new QuestionViewModel(question));
                        }
                        else
                            return;
                    }
                }
                else
                    return;

                await itemData.ApplyData(Document);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void NextSearchResult_Executed(object arg)
        {
            SearchResults.Index++;
            if (SearchResults.Index == SearchResults.Results.Count)
                SearchResults.Index = 0;

            Navigate.Execute(SearchResults.Results[SearchResults.Index]);
        }

        private void PreviousSearchResult_Executed(object arg)
        {
            SearchResults.Index--;
            if (SearchResults.Index == -1)
                SearchResults.Index = SearchResults.Results.Count - 1;

            Navigate.Execute(SearchResults.Results[SearchResults.Index]);
        }

        private void ClearSearchText_Executed(object arg)
        {
            SearchText = "";
        }

        private async void ImportSiq_Executed(object arg)
        {
            var files = PlatformManager.Instance.ShowOpenUI();
            if (files != null)
            {
                BeginChange();

                try
                {
                    foreach (var file in files)
                    {
                        using (var stream = File.OpenRead(file))
                        {
                            using (var doc = SIDocument.Load(stream))
                            {
                                foreach (var round in doc.Package.Rounds)
                                {
                                    Package.Rounds.Add(new RoundViewModel(round.Clone()));
                                }

                                CopyAuthorsAndSources(doc, doc.Package);
                                
                                foreach (var round in doc.Package.Rounds)
                                {
                                    CopyAuthorsAndSources(doc, round);
                                    foreach (var theme in round.Themes)
                                    {
                                        CopyAuthorsAndSources(doc, theme);
                                        foreach (var question in theme.Questions)
                                        {
                                            CopyAuthorsAndSources(doc, question);
                                            foreach (var atom in question.Scenario)
                                            {
                                                if (atom.Type != AtomTypes.Image && atom.Type != AtomTypes.Audio && atom.Type != AtomTypes.Video)
                                                {
                                                    continue;
                                                }

                                                var collection = doc.Images;
                                                var newCollection = Images;
                                                switch (atom.Type)
                                                {
                                                    case AtomTypes.Audio:
                                                        collection = doc.Audio;
                                                        newCollection = Audio;
                                                        break;

                                                    case AtomTypes.Video:
                                                        collection = doc.Video;
                                                        newCollection = Video;
                                                        break;
                                                }

                                                var link = doc.GetLink(atom);

                                                if (link.GetStream != null && !newCollection.Files.Any(f => f.Model.Name == link.Uri))
                                                {
                                                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                                                    Directory.CreateDirectory(tempPath);
                                                    var tempFile = System.IO.Path.Combine(tempPath, link.Uri);
                                                    using (var fileStream = File.Create(tempFile))
                                                    {
                                                        await link.GetStream().Stream.CopyToAsync(fileStream);
                                                    }

                                                    newCollection.AddFile(tempFile);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    CommitChange();
                }
                catch (Exception exc)
                {
                    RollbackChange();
                    OnError(exc);
                }
            }
        }

        private void CopyAuthorsAndSources(SIDocument document, InfoOwner infoOwner)
        {
            var length = infoOwner.Info.Authors.Count;
            for (int i = 0; i < length; i++)
            {
                var otherAuthor = document.GetLink(infoOwner.Info.Authors, i);
                if (otherAuthor != null)
                {
                    if (Authors.Collection.All(author => author.Id != otherAuthor.Id))
                    {
                        Authors.Collection.Add(otherAuthor.Clone());
                    }
                }
            }

            length = infoOwner.Info.Sources.Count;
            for (int i = 0; i < length; i++)
            {
                var otherSource = document.GetLink(infoOwner.Info.Sources, i);
                if (otherSource != null)
                {
                    if (Sources.Collection.All(source => source.Id != otherSource.Id))
                    {
                        Sources.Collection.Add(otherSource.Clone());
                    }
                }
            }
        }

        private async Task Save_Executed(object arg)
        {
            try
            {
                if (OverridePath != null)
                {
                    await SaveAsInternal(OverridePath);
                    OverridePath = null;

                    File.Delete(OriginalPath);
                    OriginalPath = null;
                }
                else if (_path.Length > 0)
                    await SaveInternal();
                else
                    await SaveAs();
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        #region Export

        /// <summary>
        /// Преобразовать пакет в HTML с помощью XSLT
        /// </summary>
        /// <param name="xslt">Путь к XSLT файлу</param>
        internal void TransformPackage(string xslt)
        {
            try
            {
                var transform = new XslCompiledTransform();
                transform.Load(xslt);

                string filename = null;
                var filter = new Dictionary<string, string>
                {
                    ["HTML файлы"] = "html"
                };

                if (PlatformManager.Instance.ShowSaveUI(Resources.Transform, "html", filter, ref filename))
                {
                    using (var ms = new MemoryStream())
                    {
                        Document.SaveXml(ms);
                        ms.Position = 0;

                        using (var xreader = XmlReader.Create(ms))
                        using (var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Write))
                        {
                            using (var result = XmlWriter.Create(fs, new XmlWriterSettings { OmitXmlDeclaration = true }))
                            {
                                transform.Transform(xreader, result);
                            }
                        }
                    }

                    Process.Start(filename);
                }
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void ExportHtml_Executed(object arg)
        {
            TransformPackage(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ygpackagekey3.0.xslt"));
        }

        private void ExportPrintHtml_Executed(object arg)
        {
            TransformPackage(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ygpackagesimple3.0.xslt"));
        }

        private void ExportFormattedHtml_Executed(object arg)
        { 
            Dialog = new ExportHtmlViewModel(this);
        }

        private void ExportBase_Executed(object arg)
        {
            Dialog = new ExportViewModel(this, ExportFormats.Db);
        }

        private void ExportMirc_Executed(object arg)
        {
            try
            {
                string filename = String.Format("{0}IRCScriptFile", FileName.Replace(".", "-"));
                var filter = new Dictionary<string, string>
                {
                    ["Текстовые файлы"] = "txt"
                };
                if (PlatformManager.Instance.ShowSaveUI(Resources.ToIRC, "txt", filter, ref filename))
                {
                    var file = new StringBuilder();
                    file.AppendLine(Document.Package.Rounds.Count.ToString());
                    Document.Package.Rounds.ForEach(round =>
                    {
                        file.AppendLine(round.Themes.Count.ToString());
                        round.Themes.ForEach(theme => file.AppendLine(theme.Questions.Count.ToString()));
                    });

                    file.AppendLine();
                    file.AppendLine(Resources.ToIRCtext);
                    file.AppendLine();
                    int pind = 1, rind = 1, tind = 1, qind = 1;
                    file.AppendLine(String.Format("[p{0}name]", pind));
                    file.AppendLine(Document.Package.Name);

                    file.AppendLine(String.Format("[p{0}auth]", pind));
                    file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(Document.Package.Info.Authors)));

                    file.AppendLine(String.Format("[p{0}sour]", pind));
                    file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(Document.Package.Info.Sources)));

                    file.AppendLine(String.Format("[p{0}comm]", pind));
                    file.AppendLine(Document.Package.Info.Comments.Text.GrowFirstLetter().EndWithPoint());

                    Document.Package.Rounds.ForEach(round =>
                    {
                        file.AppendLine(String.Format("[r{0}name]", rind));
                        file.AppendLine(round.Name);
                        file.AppendLine(String.Format("[r{0}type]", rind));
                        if (round.Type == RoundTypes.Standart)
                            file.AppendLine(Resources.Simple);
                        else
                            file.AppendLine(Resources.Final);
                        file.AppendLine(String.Format("[r{0}auth]", rind));
                        file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(round.Info.Authors)));
                        file.AppendLine(String.Format("[r{0}sour]", rind));
                        file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(round.Info.Sources)));
                        file.AppendLine(String.Format("[r{0}comm]", rind));
                        file.AppendLine(round.Info.Comments.Text.GrowFirstLetter().EndWithPoint());
                        round.Themes.ForEach(theme =>
                        {
                            file.AppendLine(String.Format("[t{0}name]", tind));
                            file.AppendLine(theme.Name);
                            file.AppendLine(String.Format("[t{0}auth]", tind));
                            file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(theme.Info.Authors)));
                            file.AppendLine(String.Format("[t{0}sour]", tind));
                            file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(theme.Info.Sources)));
                            file.AppendLine(String.Format("[t{0}comm]", tind));
                            file.AppendLine(theme.Info.Comments.Text.GrowFirstLetter().EndWithPoint());
                            theme.Questions.ForEach(quest =>
                            {
                                file.AppendLine(String.Format("[q{0}price]", qind));
                                file.AppendLine(quest.Price.ToString());
                                file.AppendLine(String.Format("[q{0}type]", qind));
                                file.AppendLine(quest.Type.Name);
                                foreach (QuestionTypeParam p in quest.Type.Params)
                                {
                                    file.AppendLine(String.Format("[q{0}{1}]", qind, p.Name));
                                    file.AppendLine(p.Value.Replace('[', '<').Replace(']', '>'));
                                }
                                var qText = new StringBuilder();
                                var showmanComments = new StringBuilder();

                                foreach (var item in quest.Scenario)
                                {
                                    if (item.Type == AtomTypes.Image)
                                    {
                                        if (showmanComments.Length > 0)
                                            showmanComments.AppendLine();
                                        showmanComments.Append("* изображение: ");
                                        showmanComments.Append(item.Text);
                                    }
                                    else if (item.Type == AtomTypes.Audio)
                                    {
                                        if (showmanComments.Length > 0)
                                            showmanComments.AppendLine();
                                        showmanComments.Append("* звук: ");
                                        showmanComments.Append(item.Text);
                                    }
                                    else if (item.Type == AtomTypes.Video)
                                    {
                                        if (showmanComments.Length > 0)
                                            showmanComments.AppendLine();
                                        showmanComments.Append("* видео: ");
                                        showmanComments.Append(item.Text);
                                    }
                                    else
                                    {
                                        if (qText.Length > 0)
                                            qText.AppendLine();
                                        qText.Append(item.Text);
                                    }
                                }

                                var comments = quest.Info.Comments.Text.GrowFirstLetter().EndWithPoint();
                                if (showmanComments.Length == 0 || comments.Length > 0)
                                {
                                    if (showmanComments.Length > 0)
                                        showmanComments.AppendLine();
                                    showmanComments.Append(comments);
                                }

                                file.AppendLine(String.Format("[q{0}text]", qind));
                                file.AppendLine(qText.ToString());
                                file.AppendLine(String.Format("[q{0}right]", qind));
                                file.AppendLine(string.Join(Environment.NewLine, quest.Right.ToArray()));
                                file.AppendLine(String.Format("[q{0}wrong]", qind));
                                file.AppendLine(string.Join(Environment.NewLine, quest.Wrong.ToArray()));
                                file.AppendLine(String.Format("[q{0}auth]", qind));
                                file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(quest.Info.Authors)));
                                file.AppendLine(String.Format("[q{0}sour]", qind));
                                file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(quest.Info.Sources)));
                                file.AppendLine(String.Format("[q{0}comm]", qind));
                                file.AppendLine(showmanComments.ToString());
                                qind++;
                            });
                            tind++;
                        });
                        rind++;
                    });

                    using (var writer = new StreamWriter(filename, false, Encoding.GetEncoding(1251)))
                    {
                        writer.Write(file);
                    }
                }
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void ExportTvSI_Executed(object arg)
        {
            Dialog = new ExportViewModel(this, ExportFormats.TvSI);
        }

        private void ExportSns_Executed(object arg)
        {
            Dialog = new ExportViewModel(this, ExportFormats.Sns);
        }

        private void ExportDinabank_Executed(object arg)
        {
            Dialog = new ExportViewModel(this, ExportFormats.Dinabank);
        }

        private async void ExportTable_Executed(object arg)
        {
            string filename = FileName.Replace(".", "-");
            var filter = new Dictionary<string, string>
            {
                ["Документы (*.xps)"] = "xps"
            };
            if (PlatformManager.Instance.ShowSaveUI("Экспорт в формат таблицы", "xps", filter, ref filename))
            {
                IsProgress = true;
                try
                {
                    await Task.Run(() => PlatformManager.Instance.ExportTable(Document, filename));
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
                finally
                {
                    IsProgress = false;
                }
            }
        }

        #endregion

        private void Navigate_Executed(object arg)
        {
            if (_activeNode != null)
                _activeNode.IsSelected = false;

            if (arg == null)
            {
                Package.IsSelected = true;
                ActiveNode = Package;
                ActiveItem = null;
                return;
            }

            var infoOwner = (IItemViewModel)arg;
            var parent = infoOwner.Owner;
            while (parent != null) // Раскрываем донизу
            {
                parent.IsExpanded = true;
                parent = parent.Owner;
            }

            infoOwner.IsSelected = true;
            ActiveNode = infoOwner;
            ActiveItem = null;
        }

        internal async Task SaveInternal()
        {
            if (!await SetLock())
                return;

            try
            {
                // Сначала сохраним во временный файл (для надёжности)
                // А то бывают случаи, когда сохранённый файл оказывался нечитаем
                var tempPath = System.IO.Path.GetTempFileName();
                var tempStream = File.Open(tempPath, FileMode.Open, FileAccess.ReadWrite);

                Document.SaveAs(tempStream, true);

                if (Images.HasPendingChanges)
                    await Images.CommitAsync(Document.Images);

                if (Audio.HasPendingChanges)
                    await Audio.CommitAsync(Document.Audio);

                if (Video.HasPendingChanges)
                    await Video.CommitAsync(Document.Video);

                Document.Dispose();
                // Проверим качество сохранения

                var testStream = File.Open(tempPath, FileMode.Open, FileAccess.Read);
                using (SIDocument.Load(testStream)) { }

                File.Copy(tempPath, _path, true);
                File.Delete(tempPath);

                Changed = false;

                ClearTempFile(_path);

                var stream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                Document.ResetTo(stream);
            }
            finally
            {
                SetUnlock();
            }
        }

        internal async Task SaveAsInternal(string path)
        {
            if (!await SetLock())
                return;

            FileStream stream = null;
            try
            {
                stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
                Document.SaveAs(stream, true);

                if (Images.HasPendingChanges)
                    await Images.CommitAsync(Document.Images);

                if (Audio.HasPendingChanges)
                    await Audio.CommitAsync(Document.Audio);

                if (Video.HasPendingChanges)
                    await Video.CommitAsync(Document.Video);

                Document.Dispose();
                ClearTempFile(this._path);

                Path = path;
                Changed = false;

                FileName = System.IO.Path.GetFileNameWithoutExtension(this._path);

                stream = File.Open(this._path, FileMode.Open, FileAccess.ReadWrite);
                Document.ResetTo(stream);
            }
            catch (Exception exc)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                throw exc;
            }
            finally
            {
                SetUnlock();
            }
        }

        private void ClearTempFile(string path)
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SIQuester", EncodePath(path));
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
            }
        }

        private async void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await SaveAs();
        }

        private async Task SaveAs()
        {
            try
            {
                string filename = Document.Package.Name;
                var filter = new Dictionary<string, string>
                {
                    ["Вопросы СИ"] = "siq"
                };

                if (PlatformManager.Instance.ShowSaveUI(null, "siq", filter, ref filename))
                {
                    await SaveAsInternal(filename);
                    AppSettings.Default.History.Add(filename);
                }
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        /// <summary>
        /// Начать комплексное изменение
        /// </summary>
        public void BeginChange()
        {
            if (_changeGroup != null)
                OnError(new Exception(Resources.ChangeGroupIsActivated));

            _changeGroup = new ChangeGroup();
        }

        /// <summary>
        /// Завершить комплексное изменение
        /// </summary>
        public void CommitChange()
        {
            if (_changeGroup != null && _changeGroup.Count > 0)
            {
                _undoList.Push(_changeGroup);
                CheckUndoCanBeExecuted();
                Changed = true;
            }

            _changeGroup = null;
            _redoList.Clear();
            CheckRedoCanBeExecuted();
        }

        internal void Search(string query, CancellationToken token)
        {
            SearchResults = new SearchResults() { Query = query };
            var package = Package;
            if (package.Model.Contains(query))
            {
                lock (_searchSync)
                {
                    if (token.IsCancellationRequested)
                        return;

                    SearchResults.Results.Add(package);
                }
            }

            foreach (var round in package.Rounds)
            {
                if (round.Model.Contains(query))
                {
                    lock (_searchSync)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        SearchResults.Results.Add(round);
                    }
                }

                foreach (var theme in round.Themes)
                {
                    if (theme.Model.Contains(query))
                    {
                        lock (_searchSync)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            SearchResults.Results.Add(theme);
                        }
                    }

                    foreach (var quest in theme.Questions)
                    {
                        if (quest.Model.Contains(query))
                        {
                            lock (_searchSync)
                            {
                                if (token.IsCancellationRequested)
                                    return;

                                SearchResults.Results.Add(quest);
                            }
                        }
                    }
                }
            }
        }

        public void RollbackChange()
        {
            _changeGroup = null;
        }

        protected override async Task Close_Executed(object arg)
        {
            try
            {
                if (NeedSave())
                {
                    var message = string.Format(Resources.DoYouWantToSave, FileName);

                    var result = arg == null ?
                        PlatformManager.Instance.ConfirmWithCancel(message)
                        : PlatformManager.Instance.Confirm(message);

                    if (!result.HasValue)
                        return;

                    if (result.Value)
                    {
                        try
                        {
                            await Save_Executed(null);
                            if (NeedSave())
                                return;
                        }
                        catch (Exception exc)
                        {
                            OnError(exc);
                            return;
                        }
                    }
                }

                OnClosed();
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void Undo_Executed(object arg)
        {
            try
            {
                _isMakingUndo = true;
                var item = _undoList.Pop();
                CheckUndoCanBeExecuted();
                item.Undo();

                _redoList.Push(item);
                CheckRedoCanBeExecuted();
                _isMakingUndo = false;

                Changed = true;
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void Redo_Executed(object arg)
        {
            try
            {
                _isMakingUndo = true;
                var item = _redoList.Pop();
                CheckRedoCanBeExecuted();
                item.Redo();

                _undoList.Push(item);
                CheckUndoCanBeExecuted();
                _isMakingUndo = false;

                Changed = true;
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void CheckUndoCanBeExecuted()
        {
            Undo.CanBeExecuted = _undoList.Any();
        }

        private void CheckRedoCanBeExecuted()
        {
            Redo.CanBeExecuted = _redoList.Any();
        }

        private async void Wikify_Executed(object arg)
        {
            IsProgress = true;
            BeginChange();
            try
            {
                await Task.Run(WikifyAsync);
                CommitChange();
            }
            catch (Exception exc)
            {
                RollbackChange();
                OnError(exc);
            }
            finally
            {
                IsProgress = false;
            }
        }

        private void WikifyAsync()
        {
            WikifyInfoOwner(Package);
            foreach (var round in Package.Rounds)
            {
                WikifyInfoOwner(round);
                foreach (var theme in round.Themes)
                {
                    WikifyInfoOwner(theme);
                    theme.Model.Name = theme.Model.Name.Wikify();
                    foreach (var quest in theme.Questions)
                    {
                        foreach (var atom in quest.Scenario)
                            if (atom.Model.Type == AtomTypes.Text)
                                atom.Model.Text = atom.Model.Text.Wikify();

                        for (int i = 0; i < quest.Right.Count; i++)
                        {
                            var value = quest.Right[i];
                            var newValue = value.Wikify();
                            if (newValue != value)
                            {
                                var index = i;
                                Task.Factory.StartNew(() => { quest.Right[index] = newValue; }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                            }
                        }

                        for (int i = 0; i < quest.Wrong.Count; i++)
                        {
                            var value = quest.Wrong[i];
                            var newValue = value.Wikify();
                            if (newValue != value)
                            {
                                var index = i;
                                Task.Factory.StartNew(() => { quest.Wrong[index] = newValue; }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                            }
                        }

                        WikifyInfoOwner(quest);
                    }
                }
            }
        }

        private static void WikifyInfoOwner(IItemViewModel owner)
        {
            var info = owner.Info;
            for (int i = 0; i < info.Authors.Count; i++)
            {
                var value = info.Authors[i];
                var newValue = value.Wikify().GrowFirstLetter();
                if (newValue != value)
                {
                    var index = i;
                    Task.Factory.StartNew(() => { info.Authors[index] = newValue; }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                }
            }

            for (int i = 0; i < info.Sources.Count; i++)
            {
                var value = info.Sources[i];
                var newValue = value.Wikify().GrowFirstLetter();
                if (newValue != value)
                {
                    var index = i;
                    Task.Factory.StartNew(() => { info.Sources[index] = newValue; }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                }
            }

            info.Comments.Text = info.Comments.Text.Wikify().ClearPoints();
        }

        #region Convert

        private void ConvertToCompTvSI_Executed(object arg)
        {
            var allthemes = new List<ThemeViewModel>();
            BeginChange();

            try
            {
                foreach (var round in Package.Rounds)
                {
                    foreach (var theme in round.Themes)
                    {
                        allthemes.Add(theme);
                    }
                }

                while (Package.Rounds.Count > 0)
                    Package.Rounds.RemoveAt(0);

                int ind = 0;

                allthemes.ForEach(theme =>
                {
                    theme.Model.Name = theme.Model.Name?.ToUpper().ClearPoints();
                    theme.OwnerRound = null;
                });

                for (int i = 0; i < 3; i++)
                {
                    var round = new Round { Name = (Package.Rounds.Count + 1).ToString() + Resources.EndingRound, Type = RoundTypes.Standart };
                    var roundViewModel = new RoundViewModel(round) { IsExpanded = true };
                    Package.Rounds.Add(roundViewModel);

                    for (int j = 0; j < 6; j++)
                    {
                        ThemeViewModel themeViewModel;
                        if (allthemes.Count == ind)
                            themeViewModel = new ThemeViewModel(new Theme());
                        else
                            themeViewModel = allthemes[ind++];

                        roundViewModel.Themes.Add(themeViewModel);
                        for (int k = 0; k < 5; k++)
                        {
                            QuestionViewModel questionViewModel;
                            if (themeViewModel.Questions.Count <= k)
                            {
                                var question = CreateQuestion(100 * (i + 1) * (k + 1));

                                questionViewModel = new QuestionViewModel(question);
                                themeViewModel.Questions.Add(questionViewModel);
                            }
                            else
                            {
                                questionViewModel = themeViewModel.Questions[k];
                                questionViewModel.Model.Price = (100 * (i + 1) * (k + 1));
                            }

                            foreach (var atom in questionViewModel.Scenario)
                            {
                                if (atom.Model.Type == AtomTypes.Text)
                                    atom.Model.Text = atom.Model.Text.ClearPoints().GrowFirstLetter();
                            }

                            questionViewModel.Right[0] = questionViewModel.Right[0].ClearPoints().GrowFirstLetter();
                        }
                    }
                }

                var finalViewModel = new RoundViewModel(new Round { Type = RoundTypes.Final, Name = Resources.Final }) { IsExpanded = true };
                Package.Rounds.Add(finalViewModel);

                for (int j = 0; j < 7; j++)
                {
                    ThemeViewModel themeViewModel;
                    if (allthemes.Count == ind)
                        themeViewModel = new ThemeViewModel(new Theme());
                    else
                        themeViewModel = allthemes[ind++];

                    finalViewModel.Themes.Add(themeViewModel);

                    QuestionViewModel questionViewModel;
                    if (themeViewModel.Questions.Count == 0)
                    {
                        var question = CreateQuestion(0);

                        questionViewModel = new QuestionViewModel(question);
                        themeViewModel.Questions.Add(questionViewModel);
                    }
                    else
                    {
                        questionViewModel = themeViewModel.Questions[0];
                        questionViewModel.Model.Price = 0;
                    }

                    foreach (var atom in questionViewModel.Scenario)
                        atom.Model.Text = atom.Model.Text.ClearPoints();

                    questionViewModel.Right[0] = questionViewModel.Right[0].ClearPoints().GrowFirstLetter();
                }

                if (ind < allthemes.Count)
                {
                    var otherViewModel = new RoundViewModel(new Round { Type = RoundTypes.Standart, Name = Resources.Rest });
                    Package.Rounds.Add(otherViewModel);
                    while (ind < allthemes.Count)
                        otherViewModel.Themes.Add(allthemes[ind++]);
                }
            }
            finally
            {
                CommitChange();
            }
        }

        private void ConvertToCompTvSISimple_Executed(object arg)
        {
            BeginChange();

            try
            {
                WikifyInfoOwner(Package);
                foreach (var round in Package.Rounds)
                {
                    WikifyInfoOwner(round);
                    foreach (var theme in round.Themes)
                    {
                        var name = theme.Model.Name;
                        if (name != null)
                            theme.Model.Name = name.Wikify().ClearPoints(); // решил больше не менять регистр

                        // Возможно, стоит заменить авторов вопроса единым автором у темы
                        var singleAuthor = theme.Questions.Count > 0 && theme.Questions[0].Info.Authors.Count == 1 ? theme.Questions[0].Info.Authors[0] : null;
                        if (singleAuthor != null)
                        {
                            var allAuthorsTheSame = true;
                            for (int i = 1; i < theme.Questions.Count; i++)
                            {
                                var authors = theme.Questions[i].Info.Authors;
                                if (authors.Count != 1 || authors[0] != singleAuthor)
                                {
                                    allAuthorsTheSame = false;
                                    break;
                                }
                            }

                            if (allAuthorsTheSame)
                            {
                                for (int i = 0; i < theme.Questions.Count; i++)
                                {
                                    theme.Questions[i].Info.Authors.RemoveAt(0);
                                }

                                theme.Info.Authors.Add(singleAuthor);
                            }
                        }

                        var singleAtom = theme.Questions.Count > 1 && theme.Questions[0].Scenario.Count > 1 ? theme.Questions[0].Scenario[0].Model.Text : null;
                        if (singleAtom != null)
                        {
                            var allAtomsTheSame = true;
                            for (int i = 1; i < theme.Questions.Count; i++)
                            {
                                var scenario = theme.Questions[i].Scenario;
                                if (scenario.Count < 2 || scenario[0].Model.Text != singleAtom)
                                {
                                    allAtomsTheSame = false;
                                    break;
                                }
                            }

                            if (allAtomsTheSame)
                            {
                                for (int i = 0; i < theme.Questions.Count; i++)
                                {
                                    theme.Questions[i].Scenario.RemoveAt(0);
                                }

                                theme.Info.Comments.Text += singleAtom;
                            }
                        }

                        WikifyInfoOwner(theme);

                        foreach (var quest in theme.Questions)
                        {
                            WikifyInfoOwner(quest);

                            for (int i = 0; i < quest.Right.Count; i++)
                            {
                                var right = quest.Right[i].Wikify().ClearPoints().GrowFirstLetter();
                                var index = right.IndexOf('/');

                                if (index > 0 && index < right.Length - 1)
                                {
                                    // Расщепим ответ на два
                                    quest.Right[i] = right.Substring(0, index).TrimEnd();
                                    quest.Right.Add(right.Substring(index + 1).TrimStart());
                                }
                                else
                                    quest.Right[i] = right;
                            }

                            for (int i = 0; i < quest.Wrong.Count; i++)
                                quest.Wrong[i] = quest.Wrong[i].Wikify().ClearPoints().GrowFirstLetter();

                            foreach (var atom in quest.Scenario)
                            {
                                if (atom.Model.Type == AtomTypes.Text)
                                {
                                    var text = atom.Model.Text;
                                    if (text != null)
                                        atom.Model.Text = text.Wikify().ClearPoints().GrowFirstLetter();
                                }
                            }

                            if (quest.Type.Name == QuestionTypes.Cat || quest.Type.Name == QuestionTypes.BagCat)
                            {
                                foreach (var param in quest.Type.Params)
                                {
                                    if (param.Model.Name == QuestionTypeParams.Cat_Theme)
                                    {
                                        var val = param.Model.Value;
                                        if (val != null)
                                            param.Model.Value = val.Wikify().ToUpper().ClearPoints();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                CommitChange();
            }
        }

        internal static Question CreateQuestion(int price)
        {
            var question = new Question { Price = price };

            var atom = new Atom();
            question.Scenario.Add(atom);            
            question.Right.Add("");

            return question;
        }

        private void ConvertToSportSI_Executed(object arg)
        {
            BeginChange();
            try
            {
                foreach (var round in Document.Package.Rounds)
                {
                    foreach (var theme in round.Themes)
                    {
                        for (int i = 0; i < theme.Questions.Count; i++)
                            theme.Questions[i].Price = 10 * (i + 1);
                    }
                }
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
            finally
            {
                CommitChange();
            }
        }

        private void ConvertToMillionaire_Executed(object arg)
        {
            BeginChange();
            var allq = new List<QuestionViewModel>();

            try
            {
                foreach (var round in Package.Rounds)
                {
                    foreach (var theme in round.Themes)
                    {
                        allq.AddRange(theme.Questions);

                        while (theme.Questions.Any())
                            theme.Questions.RemoveAt(0);
                    }

                    while (round.Themes.Count > 0)
                        round.Themes.RemoveAt(0);
                }

                while (Package.Rounds.Count > 0)
                    Package.Rounds.RemoveAt(0);

                var ind = 0;

                var gamesCount = (int)Math.Floor((double)allq.Count / 15);

                var roundViewModel = new RoundViewModel(new Round { Type = RoundTypes.Standart, Name = Resources.ThemesCollection }) { IsExpanded = true };
                Package.Rounds.Add(roundViewModel);

                for (int i = 0; i < gamesCount; i++)
                {
                    var themeViewModel = new ThemeViewModel(new Theme { Name = String.Format(Resources.GameNumber, i + 1) });
                    roundViewModel.Themes.Add(themeViewModel);

                    for (int j = 0; j < 15; j++)
                    {
                        int price;
                        if (j == 0) price = 500;
                        else if (j == 1) price = 1000;
                        else if (j == 2) price = 2000;
                        else if (j == 3) price = 3000;
                        else if (j == 4) price = 5000;
                        else if (j == 5) price = 10000;
                        else if (j == 6) price = 15000;
                        else if (j == 7) price = 25000;
                        else if (j == 8) price = 50000;
                        else if (j == 9) price = 100000;
                        else if (j == 10) price = 200000;
                        else if (j == 11) price = 400000;
                        else if (j == 12) price = 800000;
                        else if (j == 13) price = 1500000;
                        else price = 3000000;

                        allq[ind + j].Model.Price = price;
                        themeViewModel.Questions.Add(allq[ind + j]);
                    }

                    ind += 15;
                }

                if (ind < allq.Count)
                {
                    var themeViewModel = new ThemeViewModel(new Theme { Name = Resources.Left });
                    roundViewModel.Themes.Add(themeViewModel);
                    while (ind < allq.Count)
                        themeViewModel.Questions.Add(allq[ind++]);
                }
            }
            finally
            {
                CommitChange();
            }
        }

        #endregion

        private void SelectThemes_Executed(object arg)
        {
            var selectThemesViewModel = new SelectThemesViewModel(this);
            selectThemesViewModel.NewItem += OnNewItem;
            Dialog = selectThemesViewModel;
        }

        private void ExpandAll_Executed(object arg)
        {
            var expand = Convert.ToBoolean(arg);
            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    theme.IsExpanded = expand;
                }

                round.IsExpanded = expand;
            }

            Package.IsExpanded = expand;
        }

        private void CollapseAllMedia_Executed(object arg)
        {
            ToggleMedia(false);
        }

        private void ToggleMedia(bool expand)
        {
            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    foreach (var quest in theme.Questions)
                    {
                        foreach (var atom in quest.Scenario)
                        {
                            atom.IsExpanded = expand;
                        }
                    }
                }
            }
        }

        private void ExpandAllMedia_Executed(object arg)
        {
            ToggleMedia(true);
        }

        private void Delete_Executed(object arg)
        {
            ActiveNode?.Remove?.Execute(null);
        }

        protected override void Dispose(bool disposing)
        {
            if (Document != null)
            {
                try
                {
                    Document.Dispose();
                }
                catch (ObjectDisposedException)
                {

                }

                PlatformManager.Instance.ClearMedia(Document.Images);
                PlatformManager.Instance.ClearMedia(Document.Audio);
                PlatformManager.Instance.ClearMedia(Document.Video);

                Document = null;
            }

            ClearTempFile(_path);

            if (OriginalPath != null)
            {
                File.Delete(OriginalPath);
                OriginalPath = null;
            }

            base.Dispose(disposing);
        }

        public string OverridePath { get; set; }

        public string OriginalPath { get; set; }

        internal IMedia Wrap(AtomViewModel atomViewModel)
        {
            var collection = Images;
            switch (atomViewModel.Model.Type)
            {
                case AtomTypes.Audio:
                    collection = Audio;
                    break;

                case AtomTypes.Video:
                    collection = Video;
                    break;
            }

            var link = atomViewModel.Model.Text;
            if (!link.StartsWith("@")) // Внешняя ссылка
                return new Media(link);

            return collection.Wrap(link.Substring(1));
        }
    }
}
