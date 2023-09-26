using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notions;
using SIPackages;
using SIPackages.Containers;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Serializers;
using SIQuester.ViewModel.Services;
using SIQuester.ViewModel.Workspaces.Dialogs;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Xsl;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

// TODO: this class is too heavy. It requires a refactoring

/// <summary>
/// Represents a document opened inside the editor.
/// </summary>
public sealed class QDocument : WorkspaceViewModel
{
    private const string ClipboardKey = "siqdata";

    private const string SIExtension = "siq";

    /// <summary>
    /// Maximum file size allowed by game server.
    /// </summary>
    private const int GameServerFileSizeLimit = 100 * 1024 * 1024;

    private const string ContentFileName = "content.xml";

    private const string ChangesFileName = "changes.json";

    /// <summary>
    /// Созданный объект
    /// </summary>
    public static object? ActivatedObject { get; set; }

    internal Lock Lock { get; }

    private bool _changed = false;

    public OperationsManager OperationsManager { get; } = new();

    private IItemViewModel? _activeNode = null;

    private IItemViewModel[]? _activeChain = null;

    public MediaStorageViewModel Images { get; private set; }

    public MediaStorageViewModel Audio { get; private set; }

    public MediaStorageViewModel Video { get; private set; }

    public MediaStorageViewModel Html { get; private set; }

    public MediaStorageViewModel? TryGetCollectionByMediaType(string mediaType) => mediaType switch
    {
        AtomTypes.Image => Images,
        AtomTypes.Audio or AtomTypes.AudioNew => Audio,
        AtomTypes.Video => Video,
        AtomTypes.Html => Html,
        _ => null,
    };

    public MediaStorageViewModel GetCollectionByMediaType(string mediaType) => TryGetCollectionByMediaType(mediaType)
        ?? throw new ArgumentException($"Invalid media type {mediaType}", nameof(mediaType));

    private readonly ILogger<QDocument> _logger;

    private bool _isProgress;

    public bool IsProgress
    {
        get => _isProgress;
        set { if (_isProgress != value) { _isProgress = value; OnPropertyChanged(); } }
    }

    private object? _dialog = null;

    public object? Dialog
    {
        get => _dialog;
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

    private void Workspace_Closed(WorkspaceViewModel obj) => Dialog = null;

    private string _searchText;

    public string SearchText
    {
        get => _searchText;
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

    private readonly object _searchSync = new();

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
        {
            return;
        }

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
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo || _isLinksClearingBlocked)
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
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo)
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
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo)
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
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo)
        {
            return;
        }

        var link = atom.Model.Text;

        if (!link.StartsWith("@")) // External link
        {
            return;
        }

        if (!HasLinksTo(link)) // Called after items removal so works properly
        {
            var collection = GetCollectionByMediaType(atom.Model.Type);
            RemoveFileByName(collection, link[1..]);
        }
    }

    internal void ClearLinks(ContentItem contentItem)
    {
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo || !contentItem.IsRef)
        {
            return;
        }

        var atomType = contentItem.Type;
        var link = contentItem.Value;

        if (!HasLinksTo(link)) // Called after items removal so works properly
        {
            var collection = GetCollectionByMediaType(atomType);
            RemoveFileByName(collection, link);
        }
    }

    private static void RemoveFileByName(MediaStorageViewModel collection, string fileName)
    {
        for (var i = 0; i < collection.Files.Count; i++)
        {
            if (collection.Files[i].Model.Name == fileName)
            {
                collection.DeleteItem.Execute(collection.Files[i]);
                break;
            }
        }
    }

    /// <summary>
    /// Checks is the link exists in document.
    /// </summary>
    private bool HasLinksTo(string link)
    {
        foreach (var round in Document.Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItem in question.GetContent())
                    {
                        if (contentItem.IsRef && contentItem.Value == link)
                        {
                            return true;
                        }
                    }

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
        get => _searchFailed;
        set { if (_searchFailed != value) { _searchFailed = value; OnPropertyChanged(); } }
    }

    #region Commands

    /// <summary>
    /// Imports another package into current.
    /// </summary>
    public ICommand ImportSiq { get; private set; }

    /// <summary>
    /// Сохранить
    /// </summary>
    public AsyncCommand Save { get; private set; }

    /// <summary>
    /// Save document as a template.
    /// </summary>
    public ICommand SaveAsTemplate { get; private set; }

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
    /// Exports document to YAML format.
    /// </summary>
    public ICommand ExportYaml { get; private set; }

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

    public ICommand PlayQuestion { get; private set; }

    public ICommand ExpandAll { get; private set; }

    public ICommand CollapseAllMedia { get; private set; }

    public ICommand ExpandAllMedia { get; private set; }

    public SimpleCommand SendToGame { get; private set; }

    public ICommand Delete { get; private set; }

    public SimpleCommand NextSearchResult { get; private set; }

    public SimpleCommand PreviousSearchResult { get; private set; }

    public SimpleCommand ClearSearchText { get; private set; }

    #endregion

    /// <summary>
    /// Полный путь к текущему узлу
    /// </summary>
    public IItemViewModel ActiveNode
    {
        get => _activeNode;
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

    private object? _activeItem;

    public object? ActiveItem
    {
        get => _activeItem;
        set
        {
            if (_activeItem != value)
            {
                _activeItem = value;
                OnPropertyChanged();
            }
        }
    }

    internal DataCollection GetCollection(string name) =>
        name switch
        {
            CollectionNames.ImagesStorageName => Document.Images,
            CollectionNames.AudioStorageName => Document.Audio,
            CollectionNames.VideoStorageName => Document.Video,
            CollectionNames.HtmlStorageName => Document.Html,
            _ => throw new ArgumentException($"Invalid collection name {name}", nameof(name))
        };

    /// <summary>
    /// Fills a chain of nodes from root to current active node.
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
        get => _activeChain;
        private set
        {
            _activeChain = value;
            OnPropertyChanged();
        }
    }

    private bool _isDisposed = false;

    public SIDocument Document { get; private set; }

    public PackageViewModel Package { get; }

    public PackageViewModel[] Packages => new PackageViewModel[] { Package };

    private string _path;

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

    private bool NeedSave() => Changed || string.IsNullOrEmpty(_path);

    protected internal override async ValueTask SaveIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (!NeedSave())
        {
            return;
        }

        await Save.ExecuteAsync(null);
    }

    protected internal override async ValueTask SaveToTempAsync(CancellationToken cancellationToken = default)
    {
        if (!_changed || _lastChangedTime <= _lastSavedTime || _path.Length == 0)
        {
            return;
        }

        await Lock.WithLockAsync(async () =>
        {
            // Autosave document to temp path
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                AppSettings.ProductName,
                AppSettings.AutoSaveSimpleFolderName,
                PathHelper.EncodePath(_path));

            Directory.CreateDirectory(path);

            var contentFileName = System.IO.Path.Combine(path, ContentFileName);

            using (var stream = File.Create(contentFileName))
            using (var writer = XmlWriter.Create(stream))
            {
                Document.Package.WriteXml(writer);
            }

            var changes = new MediaChanges
            {
                ImagesChanges = Images.GetChanges(),
                AudioChanges = Audio.GetChanges(),
                VideoChanges = Video.GetChanges(),
                HtmlChanges = Html.GetChanges(),
            };

            var changesFileName = System.IO.Path.Combine(path, ChangesFileName);
            await File.WriteAllTextAsync(changesFileName, JsonSerializer.Serialize(changes), cancellationToken);

            _logger.LogInformation("Document has been autosaved to {path}", contentFileName);

            _lastSavedTime = DateTime.Now;
        },
        cancellationToken);
    }

    private string? _filename = null;

    public string? FileName
    {
        get => _filename;
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
        get => _changed;
        set
        {
            if (_changed != value)
            {
                _changed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Header));
            }

            if (_changed)
            {
                _lastChangedTime = DateTime.Now;
            }
        }
    }

    private DateTime _lastChangedTime = DateTime.MinValue;

    private DateTime _lastSavedTime = DateTime.MinValue;

    public override string Header => $"{FileName}{(NeedSave() ? "*" : "")}";

    public SearchResults? SearchResults { get; private set; } = null;

    private AuthorsStorageViewModel _authors;

    public AuthorsStorageViewModel Authors
    {
        get
        {
            if (_authors == null)
            {
                _authors = new AuthorsStorageViewModel(this);
                _authors.Changed += OperationsManager.AddChange;
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
                _sources.Changed += OperationsManager.AddChange;
            }

            return _sources;
        }
    }

    private StatisticsViewModel _statistics;

    public StatisticsViewModel Statistics
    {
        get
        {
            _statistics ??= new StatisticsViewModel(this);
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

                    if (question.Parameters != null)
                    {
                        AttachParametersListener(question.Parameters);
                    }

                    question.Right.CollectionChanged += Object_CollectionChanged;
                    question.Wrong.CollectionChanged += Object_CollectionChanged;

                    question.TypeNameChanged += Question_TypeNameChanged;

                    Listen(question);
                }
            }
        }

        Images.HasChanged += Media_Commited;
        Audio.HasChanged += Media_Commited;
        Video.HasChanged += Media_Commited;
        Html.HasChanged += Media_Commited;
    }

    private void AttachParametersListener(StepParametersViewModel parameters)
    {
        parameters.CollectionChanged += Object_CollectionChanged;

        foreach (var parameter in parameters)
        {
            AttachParameterListeners(parameter);
        }
    }

    private void AttachParameterListeners(StepParameterRecord parameter)
    {
        parameter.Value.PropertyChanged += Object_PropertyValueChanged;
        parameter.Value.Model.PropertyChanged += Object_PropertyValueChanged;

        if (parameter.Value.ContentValue != null)
        {
            parameter.Value.ContentValue.CollectionChanged += Object_CollectionChanged;

            foreach (var item in parameter.Value.ContentValue)
            {
                item.Model.PropertyChanged += Object_PropertyValueChanged;
            }
        }
        else if (parameter.Value.NumberSetValue != null)
        {
            parameter.Value.NumberSetValue.PropertyChanged += Object_PropertyValueChanged;
        }
        else if (parameter.Value.GroupValue != null)
        {
            AttachParametersListener(parameter.Value.GroupValue);
        }
    }

    private void Question_TypeNameChanged(QuestionViewModel question, string oldValue)
    {
        if (question.Parameters == null || OperationsManager.IsMakingUndo)
        {
            return;
        }

        using var change = OperationsManager.BeginComplexChange();

        OperationsManager.AddChange(new SimplePropertyValueChange
        {
            Element = question,
            PropertyName = nameof(QuestionViewModel.TypeName),
            Value = oldValue
        });

        var requiredParametes = new List<(string, StepParameter)>();

        var typeName = question.TypeName;

        switch (typeName)
        {
            case QuestionTypes.Secret:
            case QuestionTypes.SecretPublicPrice:
                requiredParametes.Add((QuestionParameterNames.SelectionMode, new StepParameter { SimpleValue = StepParameterValues.SetAnswererSelect_ExceptCurrent }));

                requiredParametes.Add((QuestionParameterNames.Price, new StepParameter
                {
                    Type = StepParameterTypes.NumberSet,
                    NumberSetValue = new NumberSet()
                }));

                requiredParametes.Add((QuestionParameterNames.Theme, new StepParameter { SimpleValue = "" }));
                break;

            case QuestionTypes.SecretNoQuestion:
                requiredParametes.Add((QuestionParameterNames.SelectionMode, new StepParameter { SimpleValue = StepParameterValues.SetAnswererSelect_ExceptCurrent }));

                requiredParametes.Add((QuestionParameterNames.Price, new StepParameter
                {
                    Type = StepParameterTypes.NumberSet,
                    NumberSetValue = new NumberSet()
                }));
                break;

            default:
                break;
        }

        foreach (var parameter in question.Parameters.ToArray())
        {
            if (parameter.Key == QuestionParameterNames.Question || parameter.Key == QuestionParameterNames.Answer)
            {
                continue;
            }

            var existingParameter = requiredParametes.FirstOrDefault(p => p.Item1 == parameter.Key);

            if (existingParameter.Item2 != null)
            {
                requiredParametes.Remove(existingParameter);
            }
            else
            {
                question.Parameters.Remove(parameter);
            }
        }

        foreach (var parameter in requiredParametes)
        {
            question.Parameters.Insert(0, new StepParameterRecord(parameter.Item1, new StepParameterViewModel(question, parameter.Item2)));
        }

        change.Commit();
    }

    private void Object_PropertyValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (OperationsManager.IsMakingUndo || sender == null)
        {
            return;
        }

        if (e is ExtendedPropertyChangedEventArgs<int> extNumber)
        {
            OperationsManager.AddChange(new SimplePropertyValueChange
            {
                Element = sender,
                PropertyName = e.PropertyName,
                Value = extNumber.OldValue
            });
        }
        else if (e is ExtendedPropertyChangedEventArgs<NumberSetMode> extNumberSet)
        {
            var numberSetViewModel = (NumberSetEditorNewViewModel)sender;

            using var change = OperationsManager.BeginComplexChange();

            OperationsManager.AddChange(new SimplePropertyValueChange
            {
                Element = sender,
                PropertyName = e.PropertyName,
                Value = extNumberSet.OldValue
            });

            switch (numberSetViewModel.Mode)
            {
                case NumberSetMode.FixedValue:
                    numberSetViewModel.Maximum = numberSetViewModel.Minimum;
                    break;

                case NumberSetMode.MinimumOrMaximumInRound:
                    numberSetViewModel.Maximum = numberSetViewModel.Minimum = 0;
                    break;

                case NumberSetMode.Range:
                    break;

                case NumberSetMode.RangeWithStep:
                    break;

                default:
                    break;
            }

            change.Commit();
        }
        else if (e is ExtendedPropertyChangedEventArgs<TimeSpan> extTimeSpan)
        {
            OperationsManager.AddChange(new SimplePropertyValueChange
            {
                Element = sender,
                PropertyName = e.PropertyName,
                Value = extTimeSpan.OldValue
            });
        }
        else if (e is ExtendedPropertyChangedEventArgs<bool> extBool)
        {
            OperationsManager.AddChange(new SimplePropertyValueChange
            {
                Element = sender,
                PropertyName = e.PropertyName,
                Value = extBool.OldValue
            });
        }
        else
        {
            if (e is not ExtendedPropertyChangedEventArgs<string> ext)
            {
                return;
            }

            if (sender is QuestionTypeViewModel questionType)
            {
                using var change = OperationsManager.BeginComplexChange();

                OperationsManager.AddChange(new SimplePropertyValueChange
                {
                    Element = sender,
                    PropertyName = e.PropertyName,
                    Value = ext.OldValue
                });

                foreach (var param in questionType.Params)
                {
                    param.PropertyChanged -= Object_PropertyValueChanged;
                }

                var typeName = questionType.Model.Name;

                if (typeName == QuestionTypes.Cat ||
                    typeName == QuestionTypes.BagCat ||
                    typeName == QuestionTypes.Auction ||
                    typeName == QuestionTypes.Simple ||
                    typeName == QuestionTypes.Sponsored)
                {
                    while (questionType.Params.Count > 0) // Delete one be one to support undo operation (Undo reset does not work)
                    {
                        questionType.Params.RemoveAt(0);
                    }
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

                change.Commit();
            }
            else
            {
                OperationsManager.AddChange(
                    new SimplePropertyValueChange
                    {
                        Element = sender,
                        PropertyName = e.PropertyName,
                        Value = ext.OldValue
                    });
            }
        }
    }

    private void Media_Commited() => Changed = true;

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

    private void Object_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

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
                        {
                            package.Rounds.CollectionChanged += Object_CollectionChanged;
                        }
                        else
                        {
                            if (itemViewModel is RoundViewModel round)
                            {
                                round.Themes.CollectionChanged += Object_CollectionChanged;
                            }
                            else
                            {
                                if (itemViewModel is ThemeViewModel theme)
                                {
                                    theme.Questions.CollectionChanged += Object_CollectionChanged;
                                }
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

                                    if (questionViewModel.Parameters != null)
                                    {
                                        AttachParametersListener(questionViewModel.Parameters);
                                    }

                                    questionViewModel.Right.CollectionChanged += Object_CollectionChanged;
                                    questionViewModel.Wrong.CollectionChanged += Object_CollectionChanged;

                                    questionViewModel.TypeNameChanged += Question_TypeNameChanged;
                                }
                            }
                        }
                    }
                    else if (item is QuestionTypeViewModel type)
                    {
                        type.PropertyChanged += Object_PropertyValueChanged;
                        type.Params.CollectionChanged += Object_CollectionChanged;
                    }
                    else if (item is StepParameterRecord parameter)
                    {
                        AttachParameterListeners(parameter);
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
                        {
                            package.Rounds.CollectionChanged -= Object_CollectionChanged;
                        }
                        else
                        {
                            if (itemViewModel is RoundViewModel round)
                            {
                                round.Themes.CollectionChanged -= Object_CollectionChanged;
                            }
                            else
                            {
                                if (itemViewModel is ThemeViewModel theme)
                                {
                                    theme.Questions.CollectionChanged -= Object_CollectionChanged;
                                }
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

                                    if (questionViewModel.Parameters != null)
                                    {
                                        DetachParametersLsteners(questionViewModel.Parameters);
                                    }

                                    questionViewModel.Right.CollectionChanged -= Object_CollectionChanged;
                                    questionViewModel.Wrong.CollectionChanged -= Object_CollectionChanged;

                                    questionViewModel.TypeNameChanged -= Question_TypeNameChanged;
                                }
                            }
                        }
                    }
                    else if (item is QuestionTypeViewModel type)
                    {
                        type.PropertyChanged -= Object_PropertyValueChanged;
                        type.Params.CollectionChanged -= Object_CollectionChanged;
                    }
                    else if (item is StepParameterRecord parameter)
                    {
                        DetachParameterListeners(parameter);
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
                    {
                        return;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                return;
        }

        if (!OperationsManager.IsMakingUndo)
        {
            OperationsManager.AddChange(new CollectionChange((IList)sender, e));
        }
    }

    private void DetachParametersLsteners(StepParametersViewModel parameters)
    {
        parameters.CollectionChanged -= Object_CollectionChanged;

        foreach (var parameter in parameters)
        {
            DetachParameterListeners(parameter);
        }
    }

    private void DetachParameterListeners(StepParameterRecord parameter)
    {
        parameter.Value.PropertyChanged -= Object_PropertyValueChanged;
        parameter.Value.Model.PropertyChanged -= Object_PropertyValueChanged;

        if (parameter.Value.ContentValue != null)
        {
            parameter.Value.ContentValue.CollectionChanged -= Object_CollectionChanged;

            foreach (var contentItem in parameter.Value.ContentValue)
            {
                contentItem.Model.PropertyChanged -= Object_PropertyValueChanged;
            }
        }
        else if (parameter.Value.NumberSetValue != null)
        {
            parameter.Value.NumberSetValue.PropertyChanged -= Object_PropertyValueChanged;
        }
        else if (parameter.Value.GroupValue != null)
        {
            DetachParametersLsteners(parameter.Value.GroupValue);
        }
    }

    public StorageContextViewModel StorageContext { get; set; }

    private bool _isLinksClearingBlocked;

    private readonly ILoggerFactory _loggerFactory;

    internal QDocument(
        SIDocument document,
        StorageContextViewModel storageContextViewModel,
        ILoggerFactory loggerFactory)
    {
        Lock = new Lock(document.Package.Name);

        OperationsManager.Changed += OperationsManager_Changed;
        OperationsManager.Error += OperationsManager_Error;

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<QDocument>();

        StorageContext = storageContextViewModel;

        ImportSiq = new SimpleCommand(ImportSiq_Executed);
        
        Save = new AsyncCommand(Save_Executed);
        SaveAsTemplate = new AsyncCommand(SaveAsTemplate_Executed);

        ExportHtml = new SimpleCommand(ExportHtml_Executed);
        ExportPrintHtml = new SimpleCommand(ExportPrintHtml_Executed);
        ExportFormattedHtml = new SimpleCommand(ExportFormattedHtml_Executed);
        ExportBase = new SimpleCommand(ExportBase_Executed);
        ExportMirc = new SimpleCommand(ExportMirc_Executed);
        ExportTvSI = new SimpleCommand(ExportTvSI_Executed);
        ExportSns = new SimpleCommand(ExportSns_Executed);
        ExportDinabank = new SimpleCommand(ExportDinabank_Executed);
        ExportTable = new SimpleCommand(ExportTable_Executed);
        ExportYaml = new SimpleCommand(ExportYaml_Executed);

        ConvertToCompTvSI = new SimpleCommand(ConvertToCompTvSI_Executed);
        ConvertToCompTvSISimple = new SimpleCommand(ConvertToCompTvSISimple_Executed);
        ConvertToMillionaire = new SimpleCommand(ConvertToMillionaire_Executed);
        ConvertToSportSI = new SimpleCommand(ConvertToSportSI_Executed);

        Wikify = new SimpleCommand(Wikify_Executed);          

        Navigate = new SimpleCommand(Navigate_Executed);

        SelectThemes = new SimpleCommand(SelectThemes_Executed);
        PlayQuestion = new SimpleCommand(PlayQuestion_Executed);

        ExpandAll = new SimpleCommand(ExpandAll_Executed);

        CollapseAllMedia = new SimpleCommand(CollapseAllMedia_Executed);
        ExpandAllMedia = new SimpleCommand(ExpandAllMedia_Executed);

        SendToGame = new SimpleCommand(SendToGame_Executed);

        Delete = new SimpleCommand(Delete_Executed);

        NextSearchResult = new SimpleCommand(NextSearchResult_Executed) { CanBeExecuted = false };
        PreviousSearchResult = new SimpleCommand(PreviousSearchResult_Executed) { CanBeExecuted = false };
        ClearSearchText = new SimpleCommand(ClearSearchText_Executed) { CanBeExecuted = false };

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

        var msvmLogger = loggerFactory.CreateLogger<MediaStorageViewModel>();

        Images = new MediaStorageViewModel(this, Document.Images, Resources.Images, msvmLogger);
        Audio = new MediaStorageViewModel(this, Document.Audio, SIPackages.Properties.Resources.Audio, msvmLogger);
        Video = new MediaStorageViewModel(this, Document.Video, SIPackages.Properties.Resources.Video, msvmLogger);
        Html = new MediaStorageViewModel(this, Document.Html, SIPackages.Properties.Resources.Html, msvmLogger);

        Images.Changed += OperationsManager.AddChange;
        Audio.Changed += OperationsManager.AddChange;
        Video.Changed += OperationsManager.AddChange;
        Html.Changed += OperationsManager.AddChange;

        Images.Error += OnError;
        Audio.Error += OnError;
        Video.Error += OnError;
        Html.Error += OnError;

        CreatePropertyListeners();
    }

    private void OperationsManager_Error(Exception exc) => OnError(exc);

    private void OperationsManager_Changed() => Changed = true; // TODO: delegate all change logic to OperationsManager

    private async void SendToGame_Executed(object? arg)
    {
        try
        {
            await SaveIfNeededAsync();

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

    public string Validate()
    {
        if (string.IsNullOrWhiteSpace(Package.Model.ID))
        {
            Package.Model.ID = Guid.NewGuid().ToString();
        }

        return CheckLinks();
    }

    private static ICollection<string> FillFiles(MediaStorageViewModel mediaStorage, int maxFileSize, List<string> errors)
    {
        var files = new List<string>();

        foreach (var item in mediaStorage.Files)
        {
            var name = item.Model.Name;

            if (files.Contains(name))
            {
                errors.Add(string.Format(Resources.FileIsDuplicated, name));
            }
            
            if (AppSettings.Default.CheckFileSize && mediaStorage.GetLength(item.Model.Name) > maxFileSize * 1024)
            {
                errors.Add(string.Format(Resources.MediaFileTooLarge, name, maxFileSize));
            }

            files.Add(name);
        }

        return files;
    }

    /// <summary>
    /// Checks missing and unused files in document.
    /// </summary>
    internal string CheckLinks(bool allowExternal = false)
    {
        var errors = new List<string>();

        var images = FillFiles(Images, AppSettings.Default.MaxImageSizeKb, errors);
        var audio = FillFiles(Audio, AppSettings.Default.MaxAudioSizeKb, errors);
        var video = FillFiles(Video, AppSettings.Default.MaxVideoSizeKb, errors);
        var html = FillFiles(Html, AppSettings.Default.MaxHtmlSizeKb, errors);

        CheckCommonFiles(images, audio, video, html, errors);

        var (usedImages, usedAudio, usedVideo, usedHtml) = CollectUsedFiles(allowExternal, images, audio, video, html, errors);

        var extraFiles = images.Except(usedImages)
            .Union(audio).Except(usedAudio)
            .Union(video).Except(usedVideo)
            .Union(html).Except(usedHtml)
            .ToArray();

        if (extraFiles.Length > 0)
        {
            foreach (var item in extraFiles)
            {
                errors.Add(string.Format(Resources.UnusedFile, item));
            }
        }

        return string.Join(Environment.NewLine, errors);
    }

    private (ICollection<string> usedImages, ICollection<string> usedAudio, ICollection<string> usedVideo, ICollection<string> usedHtml) CollectUsedFiles(
        bool allowExternal,
        ICollection<string> images,
        ICollection<string> audio,
        ICollection<string> video,
        ICollection<string> html,
        List<string> errors)
    {
        var usedImages = new HashSet<string>();
        var usedAudio = new HashSet<string>();
        var usedVideo = new HashSet<string>();
        var usedHtml = new HashSet<string>();

        var logoItem = Document.Package.LogoItem;

        if (logoItem != null && logoItem.IsRef)
        {
            if (images.Contains(logoItem.Value))
            {
                usedImages.Add(logoItem.Value);
            }
            else
            {
                errors.Add(string.Format(Resources.MissingLogoFile, logoItem.Value));
            }
        }

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItem in question.Model.GetContent())
                    {
                        ICollection<string> collection;
                        HashSet<string> usedFiles;

                        switch (contentItem.Type)
                        {
                            case AtomTypes.Image:
                                collection = images;
                                usedFiles = usedImages;
                                break;

                            case AtomTypes.Audio:
                            case AtomTypes.AudioNew:
                                collection = audio;
                                usedFiles = usedAudio;
                                break;

                            case AtomTypes.Video:
                                collection = video;
                                usedFiles = usedVideo;
                                break;

                            case AtomTypes.Html:
                                collection = html;
                                usedFiles = usedHtml;
                                break;

                            default:
                                continue;
                        }

                        var media = Document.GetLink(contentItem);

                        if (collection.Contains(media.Uri))
                        {
                            usedFiles.Add(media.Uri);
                        }
                        else if (allowExternal && !contentItem.IsRef)
                        {
                            continue;
                        }
                        else
                        {
                            errors.Add($"{round.Model.Name}/{theme.Model.Name}/{question.Model.Price}: {Resources.MissingFile} \"{media.Uri}\"! {(allowExternal ? "" : Resources.ExternalLinksAreForbidden)}");
                        }
                    }

                    foreach (var atom in question.Scenario)
                    {
                        ICollection<string> collection;
                        HashSet<string> usedFiles;

                        switch (atom.Model.Type)
                        {
                            case AtomTypes.Image:
                                collection = images;
                                usedFiles = usedImages;
                                break;

                            case AtomTypes.Audio:
                            case AtomTypes.AudioNew:
                                collection = audio;
                                usedFiles = usedAudio;
                                break;

                            case AtomTypes.Video:
                                collection = video;
                                usedFiles = usedVideo;
                                break;

                            case AtomTypes.Html:
                                collection = html;
                                usedFiles = usedHtml;
                                break;

                            default:
                                continue;
                        }

                        var media = Document.GetLink(atom.Model);

                        if (collection.Contains(media.Uri))
                        {
                            usedFiles.Add(media.Uri);
                        }
                        else if (allowExternal && !atom.Model.Text.StartsWith("@"))
                        {
                            continue;
                        }
                        else
                        {
                            errors.Add($"{round.Model.Name}/{theme.Model.Name}/{question.Model.Price}: {Resources.MissingFile} \"{media.Uri}\"! {(allowExternal ? "" : Resources.ExternalLinksAreForbidden)}");
                        }
                    }
                }
            }
        }

        return (usedImages, usedAudio, usedVideo, usedHtml);
    }

    private static void CheckCommonFiles(ICollection<string> images, ICollection<string> audio, ICollection<string> video, ICollection<string> html, List<string> errors)
    {
        var crossList = images.Intersect(audio)
            .Union(images.Intersect(video))
            .Union(audio.Intersect(video))
            .Union(images.Intersect(html))
            .Union(audio.Intersect(html))
            .Union(video.Intersect(html))
            .ToArray();

        if (crossList.Length == 0)
        {
            return;
        }

        foreach (var item in crossList)
        {
            errors.Add(string.Format(Resources.FileInMultipleCategories, item));
        }
    }

    internal void Copy_Executed()
    {
        if (_activeNode == null)
        {
            return;
        }

        try
        {
            var itemData = new InfoOwnerData(this, _activeNode);
            Clipboard.SetData(ClipboardKey, itemData);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    internal void Paste_Executed()
    {
        if (_activeNode == null)
        {
            return;
        }

        if (!Clipboard.ContainsData(ClipboardKey))
        {
            return;
        }

        try
        {
            using var change = OperationsManager.BeginComplexChange();

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
                    {
                        return;
                    }
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
                        myTheme.OwnerRound.Themes.Insert(
                            myTheme.OwnerRound.Themes.IndexOf(myTheme),
                            new ThemeViewModel(theme));
                    }
                    else
                    {
                        return;
                    }
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
                        myQuestion.OwnerTheme.Questions.Insert(
                            myQuestion.OwnerTheme.Questions.IndexOf(myQuestion),
                            new QuestionViewModel(question));
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                return;
            }

            ApplyData(itemData);
            change.Commit();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    /// <summary>
    /// Applies data from other document into current.
    /// </summary>
    /// <param name="data">Document data to import.</param>
    public void ApplyData(InfoOwnerData data)
    {
        foreach (var author in data.Authors)
        {
            if (!Authors.Collection.Any(x => x.Id == author.Id))
            {
                Authors.Collection.Add(author);
            }
        }

        foreach (var source in data.Sources)
        {
            if (!Sources.Collection.Any(x => x.Id == source.Id))
            {
                Sources.Collection.Add(source);
            }
        }

        var tempMediaDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName);
        Directory.CreateDirectory(tempMediaDirectory);

        // TODO: correctly handle situations with media names collision in source and target packages

        foreach (var item in data.Images)
        {
            if (!Images.Files.Any(file => file.Model.Name == item.Key))
            {
                var extension = System.IO.Path.GetExtension(item.Key);
                var tmp = System.IO.Path.ChangeExtension(System.IO.Path.Combine(tempMediaDirectory, Guid.NewGuid().ToString()), extension);
                File.Copy(item.Value, tmp);

                Images.AddFile(tmp, item.Key);
            }
        }

        foreach (var item in data.Audio)
        {
            if (!Audio.Files.Any(file => file.Model.Name == item.Key))
            {
                var extension = System.IO.Path.GetExtension(item.Key);
                var tmp = System.IO.Path.ChangeExtension(System.IO.Path.Combine(tempMediaDirectory, Guid.NewGuid().ToString()), extension);
                File.Copy(item.Value, tmp);

                Audio.AddFile(tmp, item.Key);
            }
        }

        foreach (var item in data.Video)
        {
            if (!Video.Files.Any(file => file.Model.Name == item.Key))
            {
                var extension = System.IO.Path.GetExtension(item.Key);
                var tmp = System.IO.Path.ChangeExtension(System.IO.Path.Combine(tempMediaDirectory, Guid.NewGuid().ToString()), extension);
                File.Copy(item.Value, tmp);

                Video.AddFile(tmp, item.Key);
            }
        }

        foreach (var item in data.Html)
        {
            if (!Html.Files.Any(file => file.Model.Name == item.Key))
            {
                var extension = System.IO.Path.GetExtension(item.Key);
                var tmp = System.IO.Path.ChangeExtension(System.IO.Path.Combine(tempMediaDirectory, Guid.NewGuid().ToString()), extension);
                File.Copy(item.Value, tmp);

                Html.AddFile(tmp, item.Key);
            }
        }
    }

    private void NextSearchResult_Executed(object? arg)
    {
        SearchResults.Index++;

        if (SearchResults.Index == SearchResults.Results.Count)
        {
            SearchResults.Index = 0;
        }

        Navigate.Execute(SearchResults.Results[SearchResults.Index]);
    }

    private void PreviousSearchResult_Executed(object? arg)
    {
        SearchResults.Index--;

        if (SearchResults.Index == -1)
        {
            SearchResults.Index = SearchResults.Results.Count - 1;
        }

        Navigate.Execute(SearchResults.Results[SearchResults.Index]);
    }

    private void ClearSearchText_Executed(object? arg) => SearchText = "";

    private async void ImportSiq_Executed(object? arg)
    {
        var files = PlatformManager.Instance.ShowOpenUI();

        if (files == null)
        {
            return;
        }

        try
        {
            using var change = OperationsManager.BeginComplexChange();

            var contentImportTable = new Dictionary<string, string>();
            var scenarioImportTable = new Dictionary<string, string>();

            foreach (var file in files)
            {
                using var stream = File.OpenRead(file);
                using var doc = SIDocument.Load(stream);

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

                            foreach (var item in question.GetContent())
                            {
                                if (item.Type != AtomTypes.Image && item.Type != AtomTypes.Audio && item.Type != AtomTypes.AudioNew && item.Type != AtomTypes.Video && item.Type != AtomTypes.Html)
                                {
                                    continue;
                                }

                                await ImportContentItemAsync(doc, item, contentImportTable);
                            }

                            foreach (var atom in question.Scenario)
                            {
                                if (atom.Type != AtomTypes.Image && atom.Type != AtomTypes.Audio && atom.Type != AtomTypes.AudioNew && atom.Type != AtomTypes.Video && atom.Type != AtomTypes.Html)
                                {
                                    continue;
                                }

                                await ImportAtomAsync(doc, atom, scenarioImportTable);
                            }
                        }
                    }

                    Package.Rounds.Add(new RoundViewModel(round.Clone()));
                }
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private async Task ImportContentItemAsync(SIDocument doc, ContentItem contentItem, Dictionary<string, string> contentImportTable)
    {
        var collection = doc.GetCollection(contentItem.Type);
        var newCollection = GetCollectionByMediaType(contentItem.Type);

        var link = doc.GetLink(contentItem);

        if (link.GetStream == null)
        {
            return;
        }

        if (contentImportTable.TryGetValue(link.Uri, out var newName))
        {
            contentItem.Value = newName;
            return;
        }

        var fileName = FileHelper.GenerateUniqueFileName(link.Uri, name => newCollection.Files.Any(f => f.Model.Name == name));
        contentImportTable[link.Uri] = fileName;

        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            AppSettings.ProductName,
            AppSettings.TempMediaFolderName,
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempPath);

        var tempFile = System.IO.Path.Combine(tempPath, link.Uri);

        using (var fileStream = File.Create(tempFile))
        using (var mediaStream = link.GetStream().Stream)
        {
            await mediaStream.CopyToAsync(fileStream);
        }

        newCollection.AddFile(tempFile);
        contentItem.Value = fileName;
    }

    private async Task ImportAtomAsync(SIDocument doc, Atom atom, Dictionary<string, string> scenarioImportTable)
    {
        var collection = doc.GetCollection(atom.Type);
        var newCollection = GetCollectionByMediaType(atom.Type);

        var link = doc.GetLink(atom);

        if (link.GetStream == null)
        {
            return;
        }

        if (scenarioImportTable.TryGetValue(link.Uri, out var newName))
        {
            SIDocument.SetLink(atom, newName);
            return;
        }

        var fileName = FileHelper.GenerateUniqueFileName(link.Uri, name => newCollection.Files.Any(f => f.Model.Name == name));
        scenarioImportTable[link.Uri] = fileName;

        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            AppSettings.ProductName,
            AppSettings.TempMediaFolderName,
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempPath);

        var tempFile = System.IO.Path.Combine(tempPath, fileName);

        using (var fileStream = File.Create(tempFile))
        using (var mediaStream = link.GetStream().Stream)
        {
            await mediaStream.CopyToAsync(fileStream);
        }

        newCollection.AddFile(tempFile);
        SIDocument.SetLink(atom, fileName);
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

    private async Task Save_Executed(object? arg)
    {
        try
        {
            if (_path.Length > 0)
            {
                await SaveInternalAsync();
            }
            else
            {
                await SaveAsAsync();
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Saving error: {error}", exc.Message);
            OnError(exc, Resources.DocumentSavingError);
        }
    }

    private async Task SaveAsTemplate_Executed(object? arg)
    {
        try
        {
            var templateName = PlatformManager.Instance.AskText(Resources.TemplateName);

            if (string.IsNullOrWhiteSpace(templateName))
            {
                return;
            }

            var packageTemplatesRepository = PlatformManager.Instance.ServiceProvider.GetRequiredService<IPackageTemplatesRepository>();

            var templateFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppSettings.TemplatesFolderName);
            Directory.CreateDirectory(templateFolder);

            var templateFileName = System.IO.Path.ChangeExtension(Guid.NewGuid().ToString(), AppSettings.SiqExtension);
            var templatePath = System.IO.Path.Combine(templateFolder, templateFileName);

            using var stream = File.Open(templatePath, FileMode.Create, FileAccess.ReadWrite);
            using var tempDoc = Document.SaveAs(stream, false);

            if (Images.HasPendingChanges)
            {
                await Images.ApplyToAsync(tempDoc.Images);
            }

            if (Audio.HasPendingChanges)
            {
                await Audio.ApplyToAsync(tempDoc.Audio);
            }

            if (Video.HasPendingChanges)
            {
                await Video.ApplyToAsync(tempDoc.Video);
            }

            if (Html.HasPendingChanges)
            {
                await Html.ApplyToAsync(tempDoc.Html);
            }

            packageTemplatesRepository.AddTemplate(new PackageTemplate
            {
                Name = templateName,
                FileName = templateFileName
            });
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Saving error: {error}", exc.Message);
            OnError(exc, Resources.DocumentSavingError);
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

            string? filename = null;

            var filter = new Dictionary<string, string>
            {
                [Resources.HtmlFiles] = "html"
            };

            if (PlatformManager.Instance.ShowSaveUI(Resources.Transform, "html", filter, ref filename))
            {
                using (var ms = new MemoryStream())
                {
                    Document.SaveXml(ms);
                    ms.Position = 0;

                    using var xreader = XmlReader.Create(ms);
                    using var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
                    using var result = XmlWriter.Create(fs, new XmlWriterSettings { OmitXmlDeclaration = true });

                    transform.Transform(xreader, result);
                }

                Process.Start(filename);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void ExportHtml_Executed(object? arg) =>
        TransformPackage(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ygpackagekey3.0.xslt"));

    private void ExportPrintHtml_Executed(object? arg) =>
        TransformPackage(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ygpackagesimple3.0.xslt"));

    private void ExportFormattedHtml_Executed(object? arg) => Dialog = new ExportHtmlViewModel(this);

    private void ExportBase_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.Db);

    private void ExportMirc_Executed(object? arg)
    {
        try
        {
            string? filename = string.Format("{0}IRCScriptFile", FileName.Replace(".", "-"));

            var filter = new Dictionary<string, string>
            {
                [Resources.TextFiles] = "txt"
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

                file.AppendLine(string.Format("[p{0}name]", pind));
                file.AppendLine(Document.Package.Name);

                file.AppendLine(string.Format("[p{0}auth]", pind));
                file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(Document.Package.Info.Authors)));

                file.AppendLine(string.Format("[p{0}sour]", pind));
                file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(Document.Package.Info.Sources)));

                file.AppendLine(string.Format("[p{0}comm]", pind));
                file.AppendLine(Document.Package.Info.Comments.Text.GrowFirstLetter().EndWithPoint());

                Document.Package.Rounds.ForEach(round =>
                {
                    file.AppendLine(string.Format("[r{0}name]", rind));
                    file.AppendLine(round.Name);
                    file.AppendLine(string.Format("[r{0}type]", rind));

                    if (round.Type == RoundTypes.Standart)
                        file.AppendLine(Resources.Simple);
                    else
                        file.AppendLine(Resources.Final);

                    file.AppendLine(string.Format("[r{0}auth]", rind));
                    file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(round.Info.Authors)));
                    file.AppendLine(string.Format("[r{0}sour]", rind));
                    file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(round.Info.Sources)));
                    file.AppendLine(string.Format("[r{0}comm]", rind));
                    file.AppendLine(round.Info.Comments.Text.GrowFirstLetter().EndWithPoint());

                    round.Themes.ForEach(theme =>
                    {
                        file.AppendLine(string.Format("[t{0}name]", tind));
                        file.AppendLine(theme.Name);
                        file.AppendLine(string.Format("[t{0}auth]", tind));
                        file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(theme.Info.Authors)));
                        file.AppendLine(string.Format("[t{0}sour]", tind));
                        file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(theme.Info.Sources)));
                        file.AppendLine(string.Format("[t{0}comm]", tind));
                        file.AppendLine(theme.Info.Comments.Text.GrowFirstLetter().EndWithPoint());

                        theme.Questions.ForEach(question =>
                        {
                            file.AppendLine(string.Format("[q{0}price]", qind));
                            file.AppendLine(question.Price.ToString());
                            file.AppendLine(string.Format("[q{0}type]", qind));
                            file.AppendLine(question.Type.Name);

                            foreach (QuestionTypeParam p in question.Type.Params)
                            {
                                file.AppendLine(string.Format("[q{0}{1}]", qind, p.Name));
                                file.AppendLine(p.Value.Replace('[', '<').Replace(']', '>'));
                            }

                            var qText = new StringBuilder();
                            var showmanComments = new StringBuilder();

                            foreach (var item in question.GetContent())
                            {
                                switch (item.Type)
                                {
                                    case AtomTypes.Image:
                                        if (showmanComments.Length > 0)
                                        {
                                            showmanComments.AppendLine();
                                        }

                                        showmanComments.Append($"* {Resources.MircImage}: ");
                                        showmanComments.Append(item.Value);
                                        break;

                                    case AtomTypes.Audio:
                                    case AtomTypes.AudioNew:
                                        if (showmanComments.Length > 0)
                                        {
                                            showmanComments.AppendLine();
                                        }

                                        showmanComments.Append($"* {Resources.MircAudio}: ");
                                        showmanComments.Append(item.Value);
                                        break;

                                    case AtomTypes.Video:
                                        if (showmanComments.Length > 0)
                                        {
                                            showmanComments.AppendLine();
                                        }

                                        showmanComments.Append($"* {Resources.MircVideo}: ");
                                        showmanComments.Append(item.Value);
                                        break;

                                    default:
                                        if (qText.Length > 0)
                                        {
                                            qText.AppendLine();
                                        }

                                        qText.Append(item.Value);
                                        break;
                                }
                            }

                            foreach (var item in question.Scenario)
                            {
                                if (item.Type == AtomTypes.Image)
                                {
                                    if (showmanComments.Length > 0)
                                        showmanComments.AppendLine();

                                    showmanComments.Append($"* {Resources.MircImage}: ");
                                    showmanComments.Append(item.Text);
                                }
                                else if (item.Type == AtomTypes.Audio || item.Type == AtomTypes.AudioNew)
                                {
                                    if (showmanComments.Length > 0)
                                        showmanComments.AppendLine();

                                    showmanComments.Append($"* {Resources.MircAudio}: ");
                                    showmanComments.Append(item.Text);
                                }
                                else if (item.Type == AtomTypes.Video)
                                {
                                    if (showmanComments.Length > 0)
                                        showmanComments.AppendLine();

                                    showmanComments.Append($"* {Resources.MircVideo}: ");
                                    showmanComments.Append(item.Text);
                                }
                                else
                                {
                                    if (qText.Length > 0)
                                        qText.AppendLine();

                                    qText.Append(item.Text);
                                }
                            }

                            var comments = question.Info.Comments.Text.GrowFirstLetter().EndWithPoint();

                            if (showmanComments.Length == 0 || comments.Length > 0)
                            {
                                if (showmanComments.Length > 0)
                                    showmanComments.AppendLine();

                                showmanComments.Append(comments);
                            }

                            file.AppendLine(string.Format("[q{0}text]", qind));
                            file.AppendLine(qText.ToString());
                            file.AppendLine(string.Format("[q{0}right]", qind));
                            file.AppendLine(string.Join(Environment.NewLine, question.Right.ToArray()));
                            file.AppendLine(string.Format("[q{0}wrong]", qind));
                            file.AppendLine(string.Join(Environment.NewLine, question.Wrong.ToArray()));
                            file.AppendLine(string.Format("[q{0}auth]", qind));
                            file.AppendLine(string.Join(Environment.NewLine, Document.GetRealAuthors(question.Info.Authors)));
                            file.AppendLine(string.Format("[q{0}sour]", qind));
                            file.AppendLine(string.Join(Environment.NewLine, Document.GetRealSources(question.Info.Sources)));
                            file.AppendLine(string.Format("[q{0}comm]", qind));
                            file.AppendLine(showmanComments.ToString());

                            qind++;
                        });

                        tind++;
                    });

                    rind++;
                });

                using var writer = new StreamWriter(filename, false);
                writer.Write(file);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void ExportTvSI_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.TvSI);

    private void ExportSns_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.Sns);

    private void ExportDinabank_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.Dinabank);

    private async void ExportTable_Executed(object? arg)
    {
        string filename = FileName.Replace(".", "-");

        var filter = new Dictionary<string, string>
        {
            [Resources.XpsFiles] = "xps"
        };

        if (PlatformManager.Instance.ShowSaveUI(Resources.ExportTableHeader, "xps", filter, ref filename))
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

    private void ExportYaml_Executed(object? arg)
    {
        try
        {
            string? filename = System.IO.Path.ChangeExtension(_filename, "yaml");

            var filter = new Dictionary<string, string>
            {
                [Resources.YamlFiles] = "yaml"
            };

            if (PlatformManager.Instance.ShowSaveUI(null, "yaml", filter, ref filename))
            {
                using (var textWriter = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    YamlSerializer.SerializePackage(textWriter, Document.Package);
                }

                var baseFolder = System.IO.Path.GetDirectoryName(filename);

                if (baseFolder == null)
                {
                    throw new InvalidOperationException($"Wrong filename {filename} for exporting YAML");
                }

                ExportMediaCollection(Images, baseFolder);
                ExportMediaCollection(Audio, baseFolder);
                ExportMediaCollection(Video, baseFolder);
                ExportMediaCollection(Html, baseFolder);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void ExportMediaCollection(MediaStorageViewModel mediaStorageViewModel, string targetFolder)
    {
        var mediaFolder = System.IO.Path.Combine(targetFolder, mediaStorageViewModel.Name);

        if (mediaStorageViewModel.Files.Count > 0)
        {
            Directory.CreateDirectory(mediaFolder);
        }

        foreach (var item in mediaStorageViewModel.Files)
        {
            var targetFileName = System.IO.Path.Combine(mediaFolder, item.Model.Name);
            var filePath = mediaStorageViewModel.TryGetFilePath(item);

            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, targetFileName);
                }
                else
                {
                    _logger.LogWarning("ExportMediaCollection: File {filePath} not exist", filePath);
                }
            }
            else
            {
                var stream = mediaStorageViewModel.TryGetStream(item);

                if (stream != null)
                {
                    try
                    {
                        using var fs = File.Create(targetFileName);
                        stream.CopyTo(fs);
                    }
                    finally
                    {
                        stream.Dispose();
                    }
                }
                else
                {
                    _logger.LogWarning("ExportMediaCollection: Stream for item {name} of category {categoryName} not exist", item.Model.Name, mediaStorageViewModel.Name);
                }
            }
        }
    }

    #endregion

    private void Navigate_Executed(object? arg)
    {
        if (_activeNode != null)
        {
            _activeNode.IsSelected = false;
        }

        if (arg == null)
        {
            Package.IsSelected = true;
            ActiveNode = Package;
            ActiveItem = null;
            return;
        }

        var infoOwner = (IItemViewModel)arg;
        var parent = infoOwner.Owner;

        while (parent != null) // Expanding to leaf
        {
            parent.IsExpanded = true;
            parent = parent.Owner;
        }

        infoOwner.IsSelected = true;
        ActiveNode = infoOwner;
        ActiveItem = null;
    }

    internal ValueTask SaveInternalAsync() =>
         Lock.WithLockAsync(async () =>
         {
             // 1. Saving at temporary path to validate saved file first
             var tempPath = FileHelper.GenerateUniqueFilePath(System.IO.Path.ChangeExtension(_path, "tmp"));

             var tempStream = File.Open(tempPath, FileMode.CreateNew, FileAccess.ReadWrite);

             using (var tempDoc = Document.SaveAs(tempStream, false))
             {
                 if (Images.HasPendingChanges)
                 {
                     await Images.CommitAsync(tempDoc.Images);
                 }

                 if (Audio.HasPendingChanges)
                 {
                     await Audio.CommitAsync(tempDoc.Audio);
                 }

                 if (Video.HasPendingChanges)
                 {
                     await Video.CommitAsync(tempDoc.Video);
                 }

                 if (Html.HasPendingChanges)
                 {
                     await Html.CommitAsync(tempDoc.Html);
                 }
             }

             _logger.LogInformation("SaveInternalAsync: document has been saved to temp path: {path}", tempPath);

             // 2. Checking saved document
             var testStream = File.OpenRead(tempPath);
             using (SIDocument.Load(testStream)) { }

             // 3. Test ok, overwriting current file and switching to it. Underlying _path stream is closed
             Document.UpdateContainer(EmptySIPackageContainer.Instance); // reset source temporarily

             _logger.LogInformation("SaveInternalAsync: document is ready to be copied to final path: {path}", _path);

             try
             {
                 try
                 {
                     File.Replace(tempPath, _path, null); // It is possible to provide backup file on save here
                 }
                 catch (UnauthorizedAccessException exc)
                 {
                     _logger.LogWarning(exc, "SaveInternalAsync error. Switching to old saving method: {error}", exc.Message);

                     // Fallback to old unsafe method
                     File.Copy(tempPath, _path, true);
                     File.Delete(tempPath);
                 }

                 _logger.LogInformation("SaveInternalAsync: document has been validated and saved to final path: {path}", _path);

                 Changed = false;
                 ClearTempFolder();
                 CheckFileSize();
             }
             finally
             {
                 // 4. Opening new file
                 var stream = File.OpenRead(_path);
                 Document.ResetTo(stream);

                 _logger.LogInformation("SaveInternalAsync: document has been reopened: {path}", _path);
             }
         });

    internal ValueTask SaveAsInternalAsync(string path) =>
        Lock.WithLockAsync(async () =>
        {
            FileStream? stream = null;

            try
            {
                stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);

                using (var tempDoc = Document.SaveAs(stream, false))
                {
                    if (Images.HasPendingChanges)
                    {
                        await Images.CommitAsync(tempDoc.Images);
                    }

                    if (Audio.HasPendingChanges)
                    {
                        await Audio.CommitAsync(tempDoc.Audio);
                    }

                    if (Video.HasPendingChanges)
                    {
                        await Video.CommitAsync(tempDoc.Video);
                    }

                    if (Html.HasPendingChanges)
                    {
                        await Html.CommitAsync(tempDoc.Html);
                    }
                }

                _logger.LogInformation("SaveAsInternalAsync: document has been saved as {path}", path);
                ClearTempFolder();

                Path = path;
                Changed = false;

                FileName = System.IO.Path.GetFileNameWithoutExtension(_path);

                var newStream = File.OpenRead(_path);

                try
                {
                    Document.ResetTo(newStream);
                    CheckFileSize();
                }
                catch (Exception)
                {
                    newStream.Dispose();
                    throw;
                }
            }
            catch (Exception)
            {
                stream?.Dispose();
                throw;
            }
        });

    public void CheckFileSize() => ErrorMessage = GetFileSizeErrorMessage();

    private string? GetFileSizeErrorMessage()
    {
        if (!AppSettings.Default.CheckFileSize)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_path) && new FileInfo(_path).Length > GameServerFileSizeLimit)
        {
            return string.Format(Resources.FileSizeLimitExceed, GameServerFileSizeLimit);
        }

        return null;
    }

    private void ClearTempFolder()
    {
        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            AppSettings.ProductName,
            AppSettings.AutoSaveSimpleFolderName,
            PathHelper.EncodePath(_path));

        if (!Directory.Exists(tempPath))
        {
            return;
        }

        try
        {
            Directory.Delete(tempPath, true);
            _logger.LogInformation("Temporary folder deleted. Folder path: {path}", tempPath);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    internal async void SaveAs_Executed() => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
        try
        {
            string filename = Document.Package.Name;

            var filter = new Dictionary<string, string>
            {
                [Resources.SIQuestions] = SIExtension
            };

            if (PlatformManager.Instance.ShowSaveUI(null, SIExtension, filter, ref filename))
            {
                await SaveAsInternalAsync(filename);
                AppSettings.Default.History.Add(filename);
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "SavingAs error: {error}", exc.Message);
            OnError(exc);
        }
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
                {
                    return;
                }

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
                    {
                        return;
                    }

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
                        {
                            return;
                        }

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
                            {
                                return;
                            }

                            SearchResults.Results.Add(quest);
                        }
                    }
                }
            }
        }
    }

    protected override async Task Close_Executed(object? arg)
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
                {
                    return;
                }

                if (result.Value)
                {
                    try
                    {
                        _logger.LogInformation("Save on exit started: {path}", _path);

                        await Save_Executed(null);

                        if (NeedSave())
                        {
                            _logger.LogInformation("Save on exit aborted: {path}", _path);
                            return;
                        }

                        _logger.LogInformation("Save on exit completed: {path}", _path);
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

    private async void Wikify_Executed(object? arg)
    {
        IsProgress = true;

        try
        {
            using var change = OperationsManager.BeginComplexChange();

            await Task.Run(WikifyAsync);
            change.Commit();
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
                    {
                        if (atom.Model.Type == AtomTypes.Text)
                        {
                            atom.Model.Text = atom.Model.Text.Wikify();
                        }
                    }

                    for (int i = 0; i < quest.Right.Count; i++)
                    {
                        var value = quest.Right[i];
                        var newValue = value.Wikify();

                        if (newValue != value)
                        {
                            var index = i;
                            // ObservableCollection should be modified in the UI thread
                            UI.Execute(() => { quest.Right[index] = newValue; }, exc => OnError(exc));
                        }
                    }

                    for (int i = 0; i < quest.Wrong.Count; i++)
                    {
                        var value = quest.Wrong[i];
                        var newValue = value.Wikify();

                        if (newValue != value)
                        {
                            var index = i;
                            // ObservableCollection should be modified in the UI thread
                            UI.Execute(() => { quest.Wrong[index] = newValue; }, exc => OnError(exc));
                        }
                    }

                    WikifyInfoOwner(quest);
                }
            }
        }
    }

    private void WikifyInfoOwner(IItemViewModel owner)
    {
        var info = owner.Info;

        for (int i = 0; i < info.Authors.Count; i++)
        {
            var value = info.Authors[i];
            var newValue = value.Wikify().GrowFirstLetter();

            if (newValue != value)
            {
                var index = i;
                UI.Execute(() => { info.Authors[index] = newValue; }, exc => OnError(exc));
            }
        }

        for (int i = 0; i < info.Sources.Count; i++)
        {
            var value = info.Sources[i];
            var newValue = value.Wikify().GrowFirstLetter();

            if (newValue != value)
            {
                var index = i;
                UI.Execute(() => { info.Sources[index] = newValue; }, exc => OnError(exc));
            }
        }

        info.Comments.Text = info.Comments.Text.Wikify().ClearPoints();
    }

    #region Convert

    private void ConvertToCompTvSI_Executed(object? arg)
    {
        try
        {
            var upgraded = Package.IsUpgraded;

            var allthemes = new List<ThemeViewModel>();
            using var change = OperationsManager.BeginComplexChange();

            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    allthemes.Add(theme);
                }
            }
            
            while (Package.Rounds.Count > 0)
            {
                Package.Rounds.RemoveAt(0);
            }

            int ind = 0;

            allthemes.ForEach(theme =>
            {
                theme.Model.Name = theme.Model.Name?.ToUpper().ClearPoints();
                theme.OwnerRound = null;
            });

            for (var i = 0; i < 3; i++)
            {
                var round = new Round { Name = (Package.Rounds.Count + 1).ToString() + Resources.EndingRound, Type = RoundTypes.Standart };
                var roundViewModel = new RoundViewModel(round) { IsExpanded = true };
                Package.Rounds.Add(roundViewModel);

                for (int j = 0; j < 6; j++)
                {
                    ThemeViewModel themeViewModel;

                    if (allthemes.Count == ind)
                    {
                        themeViewModel = new ThemeViewModel(new Theme());
                    }
                    else
                    {
                        themeViewModel = allthemes[ind++];
                    }

                    roundViewModel.Themes.Add(themeViewModel);

                    for (var k = 0; k < 5; k++)
                    {
                        QuestionViewModel questionViewModel;

                        if (themeViewModel.Questions.Count <= k)
                        {
                            var question = PackageItemsHelper.CreateQuestion(100 * (i + 1) * (k + 1), upgraded);

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
                            {
                                atom.Model.Text = atom.Model.Text.ClearPoints().GrowFirstLetter();
                            }
                        }

                        if (questionViewModel.Right.Count > 0)
                        {
                            questionViewModel.Right[0] = questionViewModel.Right[0].ClearPoints().GrowFirstLetter();
                        }
                    }
                }
            }

            var finalViewModel = new RoundViewModel(new Round { Type = RoundTypes.Final, Name = Resources.Final }) { IsExpanded = true };
            Package.Rounds.Add(finalViewModel);

            for (var j = 0; j < 7; j++)
            {
                ThemeViewModel themeViewModel;

                if (allthemes.Count == ind)
                {
                    themeViewModel = new ThemeViewModel(new Theme());
                }
                else
                {
                    themeViewModel = allthemes[ind++];
                }

                finalViewModel.Themes.Add(themeViewModel);

                QuestionViewModel questionViewModel;

                if (themeViewModel.Questions.Count == 0)
                {
                    var question = PackageItemsHelper.CreateQuestion(0, upgraded);

                    questionViewModel = new QuestionViewModel(question);
                    themeViewModel.Questions.Add(questionViewModel);
                }
                else
                {
                    questionViewModel = themeViewModel.Questions[0];
                    questionViewModel.Model.Price = 0;
                }

                foreach (var atom in questionViewModel.Scenario)
                {
                    atom.Model.Text = atom.Model.Text.ClearPoints();
                }

                questionViewModel.Right[0] = questionViewModel.Right[0].ClearPoints().GrowFirstLetter();
            }

            if (ind < allthemes.Count)
            {
                var otherViewModel = new RoundViewModel(new Round { Type = RoundTypes.Standart, Name = Resources.Rest });
                Package.Rounds.Add(otherViewModel);

                while (ind < allthemes.Count)
                {
                    otherViewModel.Themes.Add(allthemes[ind++]);
                }
            }

            change.Commit();
        }
        catch (Exception ex)
        {
            OnError(ex, Resources.ConversionError);
        }
    }

    private void ConvertToCompTvSISimple_Executed(object? arg)
    {
        using var change = OperationsManager.BeginComplexChange();

        WikifyInfoOwner(Package);

        foreach (var round in Package.Rounds)
        {
            WikifyInfoOwner(round);

            foreach (var theme in round.Themes)
            {
                var name = theme.Model.Name;

                if (name != null)
                {
                    theme.Model.Name = name.Wikify().ClearPoints();
                }

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
                            quest.Right[i] = right[..index].TrimEnd();
                            quest.Right.Add(right[(index + 1)..].TrimStart());
                        }
                        else
                        {
                            quest.Right[i] = right;
                        }
                    }

                    for (int i = 0; i < quest.Wrong.Count; i++)
                    {
                        quest.Wrong[i] = quest.Wrong[i].Wikify().ClearPoints().GrowFirstLetter();
                    }

                    foreach (var atom in quest.Scenario)
                    {
                        if (atom.Model.Type == AtomTypes.Text)
                        {
                            var text = atom.Model.Text;

                            if (text != null)
                            {
                                atom.Model.Text = text.Wikify().ClearPoints().GrowFirstLetter();
                            }
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
                                {
                                    param.Model.Value = val.Wikify().ToUpper().ClearPoints();
                                }
                            }
                        }
                    }
                }
            }
        }

        change.Commit();
    }

    private void ConvertToSportSI_Executed(object? arg)
    {
        try
        {
            using var change = OperationsManager.BeginComplexChange();

            foreach (var round in Document.Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    for (int i = 0; i < theme.Questions.Count; i++)
                    {
                        theme.Questions[i].Price = 10 * (i + 1);
                    }
                }
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void ConvertToMillionaire_Executed(object? arg)
    {
        using var change = OperationsManager.BeginComplexChange();
        var allq = new List<QuestionViewModel>();

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                allq.AddRange(theme.Questions);

                while (theme.Questions.Any())
                {
                    theme.Questions.RemoveAt(0);
                }
            }

            while (round.Themes.Count > 0)
            {
                round.Themes.RemoveAt(0);
            }
        }

        while (Package.Rounds.Count > 0)
        {
            Package.Rounds.RemoveAt(0);
        }

        var ind = 0;

        var gamesCount = (int)Math.Floor((double)allq.Count / 15);

        var roundViewModel = new RoundViewModel(new Round { Type = RoundTypes.Standart, Name = Resources.ThemesCollection })
        {
            IsExpanded = true
        };

        Package.Rounds.Add(roundViewModel);

        for (int i = 0; i < gamesCount; i++)
        {
            var themeViewModel = new ThemeViewModel(new Theme { Name = string.Format(Resources.GameNumber, i + 1) });
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
            {
                themeViewModel.Questions.Add(allq[ind++]);
            }
        }

        change.Commit();
    }

    #endregion

    private void SelectThemes_Executed(object? arg)
    {
        var selectThemesViewModel = new SelectThemesViewModel(
            this,
            PlatformManager.Instance.ServiceProvider.GetRequiredService<IOptions<AppOptions>>().Value,
            _loggerFactory);

        selectThemesViewModel.NewItem += OnNewItem;
        Dialog = selectThemesViewModel;
    }

    private void PlayQuestion_Executed(object? arg) => Dialog = new QuestionPlayViewModel((QuestionViewModel)arg, this);

    private async void ExpandAll_Executed(object? arg)
    {
        var expand = Convert.ToBoolean(arg);

        Package.IsExpanded = expand;

        foreach (var round in Package.Rounds)
        {
            round.IsExpanded = expand;

            foreach (var theme in round.Themes)
            {
                theme.IsExpanded = expand;
                await Task.Delay(100);
            }
        }
    }

    private void CollapseAllMedia_Executed(object? arg) => ToggleMedia(false);

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

                    if (quest.Parameters != null)
                    {
                        foreach (var parameter in quest.Parameters)
                        {
                            if (parameter.Key != QuestionParameterNames.Question && parameter.Key != QuestionParameterNames.Answer
                                || parameter.Value.ContentValue == null)
                            {
                                continue;
                            }

                            foreach (var item in parameter.Value.ContentValue)
                            {
                                item.IsExpanded = expand;
                            }
                        }
                    }
                }
            }
        }
    }

    private void ExpandAllMedia_Executed(object? arg) => ToggleMedia(true);

    private void Delete_Executed(object? arg) => ActiveNode?.Remove?.Execute(null);

    /// <summary>
    /// Loads media files from provided folder.
    /// </summary>
    /// <param name="folder">Folder with media content.</param>
    internal void LoadMediaFromFolder(string folder)
    {
        if (!string.IsNullOrEmpty(Package.Model.Logo))
        {
            LoadLogo(folder);
        }

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItem in question.Model.GetContent())
                    {
                        if (!contentItem.IsRef)
                        {
                            continue;
                        }

                        var link = Document.GetLink(contentItem);
                        var mediaStorage = GetCollectionByMediaType(contentItem.Type);

                        if (mediaStorage.Files.Any(f => f.Model.Name == link.Uri))
                        {
                            continue;
                        }

                        var mediaFileName = System.IO.Path.Combine(folder, link.Uri);

                        if (!File.Exists(mediaFileName))
                        {
                            mediaFileName = System.IO.Path.Combine(folder, mediaStorage.Name, link.Uri);

                            if (!File.Exists(mediaFileName))
                            {
                                continue;
                            }
                        }

                        mediaStorage.AddFile(mediaFileName);
                    }

                    foreach (var atom in question.Scenario)
                    {
                        if (!atom.Model.IsLink)
                        {
                            continue;
                        }

                        var link = Document.GetLink(atom.Model);
                        var mediaStorage = GetCollectionByMediaType(atom.Model.Type);

                        if (mediaStorage.Files.Any(f => f.Model.Name == link.Uri))
                        {
                            continue;
                        }

                        var mediaFileName = System.IO.Path.Combine(folder, link.Uri);

                        if (!File.Exists(mediaFileName))
                        {
                            mediaFileName = System.IO.Path.Combine(folder, mediaStorage.Name, link.Uri);

                            if (!File.Exists(mediaFileName))
                            {
                                continue;
                            }
                        }

                        mediaStorage.AddFile(mediaFileName);
                    }
                }
            }
        }
    }

    private void LoadLogo(string folder)
    {
        var mediaStorage = Images;
        var logo = Package.Model.Logo[1..];

        var mediaFileName = System.IO.Path.Combine(folder, logo);

        if (!File.Exists(mediaFileName))
        {
            mediaFileName = System.IO.Path.Combine(folder, mediaStorage.Name, logo);

            if (!File.Exists(mediaFileName))
            {
                return;
            }
        }

        mediaStorage.AddFile(mediaFileName);
    }

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        Document.Dispose();

        PlatformManager.Instance.ClearMedia(Document.Images);
        PlatformManager.Instance.ClearMedia(Document.Audio);
        PlatformManager.Instance.ClearMedia(Document.Video);
        PlatformManager.Instance.ClearMedia(Document.Html);

        _logger.LogInformation("Document closed: {path}", _path);

        ClearTempFolder();

        _isDisposed = true;

        base.Dispose(disposing);
    }

    internal IMedia Wrap(Atom atom)
    {
        var link = atom.Text;

        if (!atom.IsLink) // External link
        {
            return new Media(link);
        }

        var collection = GetCollectionByMediaType(atom.Type);
        return collection.Wrap(link[1..]);
    }

    internal IMedia Wrap(ContentItem contentItem)
    {
        var link = contentItem.Value;

        if (!contentItem.IsRef) // External link
        {
            return new Media(link);
        }

        var collection = GetCollectionByMediaType(contentItem.Type);
        return collection.Wrap(link);
    }

    internal void RestoreFromFolder(DirectoryInfo folder)
    {
        var contentFile = System.IO.Path.Combine(folder.FullName, ContentFileName);
        var changesFile = System.IO.Path.Combine(folder.FullName, ChangesFileName);

        using (var change = OperationsManager.BeginComplexChange())
        {
            if (File.Exists(contentFile))
            {
                var package = new Package();

                using (var fs = File.OpenRead(contentFile))
                using (var xmlReader = XmlReader.Create(fs))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "package")
                        {
                            package.ReadXml(xmlReader);
                            break;
                        }
                    }
                }

                _isLinksClearingBlocked = true;

                try
                {
                    MergePackage(package);
                }
                finally
                {
                    _isLinksClearingBlocked = false;
                }

                Package.Info.Authors.Merge(package.Info.Authors);
                Package.Info.Sources.Merge(package.Info.Sources);
                Package.Info.Comments.Text = package.Info.Comments.Text;

                Package.Tags.Merge(package.Tags);

                Package.Model.Language = package.Language;
                Package.Model.Publisher= package.Publisher;
                Package.Model.Date = package.Date;
                Package.Model.Difficulty = package.Difficulty;
                Package.Model.Name = package.Name;
                Package.Model.Restriction = package.Restriction;
            }

            if (File.Exists(changesFile))
            {
                var changesText = File.ReadAllText(changesFile);
                var changes = JsonSerializer.Deserialize<MediaChanges>(changesText);

                _logger.LogInformation("Restore changes: {changes}", changesText);

                Images.RestoreChanges(changes.ImagesChanges);
                Audio.RestoreChanges(changes.AudioChanges);
                Video.RestoreChanges(changes.VideoChanges);
                Html.RestoreChanges(changes.HtmlChanges);
            }

            change.Commit();
        }

        folder.Delete(true);
    }

    private void MergePackage(Package package)
    {
        Package.Rounds.Merge(
            package.Rounds,
            round => new RoundViewModel(round),
            (roundViewModel, round) =>
            {
                roundViewModel.Info.Authors.Merge(round.Info.Authors);
                roundViewModel.Info.Sources.Merge(round.Info.Sources);
                roundViewModel.Info.Comments.Text = round.Info.Comments.Text;

                roundViewModel.Model.Name = round.Name;
                roundViewModel.Model.Type = round.Type;

                roundViewModel.Themes.Merge(
                    round.Themes,
                    theme => new ThemeViewModel(theme),
                    (themeViewModel, theme) =>
                    {
                        themeViewModel.Info.Authors.Merge(theme.Info.Authors);
                        themeViewModel.Info.Sources.Merge(theme.Info.Sources);
                        themeViewModel.Info.Comments.Text = theme.Info.Comments.Text;

                        themeViewModel.Model.Name = theme.Name;

                        themeViewModel.Questions.Merge(
                            theme.Questions,
                            question => new QuestionViewModel(question),
                            (questionViewModel, question) =>
                            {
                                questionViewModel.Info.Authors.Merge(question.Info.Authors);
                                questionViewModel.Info.Sources.Merge(question.Info.Sources);
                                questionViewModel.Info.Comments.Text = question.Info.Comments.Text;

                                questionViewModel.Model.Price = question.Price;
                                questionViewModel.Type.Name = question.Type.Name;

                                questionViewModel.Right.Merge(question.Right);
                                questionViewModel.Wrong.Merge(question.Wrong);

                                questionViewModel.Type.Params.Merge(
                                    question.Type.Params,
                                    p => new QuestionTypeParamViewModel(p),
                                    (vm, p) =>
                                    {
                                        vm.Model.Name = p.Name;
                                        vm.Model.Value = p.Value;
                                    });

                                if (question.Parameters != null && questionViewModel.Parameters != null)
                                {
                                    questionViewModel.Parameters.Merge(
                                        question.Parameters.ToList(),
                                        p => new StepParameterRecord(p.Key, new StepParameterViewModel(questionViewModel, p.Value)));
                                }

                                questionViewModel.Scenario.Merge(
                                    question.Scenario,
                                    atom => new AtomViewModel(atom),
                                    (atomViewModel, atom) =>
                                    {
                                        atomViewModel.Model.Type = atom.Type;
                                        atomViewModel.Model.Text = atom.Text;
                                        atomViewModel.Model.AtomTime = atom.AtomTime;
                                    });
                            });
                    });
            });
    }

    internal void RemoveUnusedFiles()
    {
        var errors = new List<string>();

        var images = FillFiles(Images, AppSettings.Default.MaxImageSizeKb, errors);
        var audio = FillFiles(Audio, AppSettings.Default.MaxAudioSizeKb, errors);
        var video = FillFiles(Video, AppSettings.Default.MaxVideoSizeKb, errors);
        var html = FillFiles(Html, AppSettings.Default.MaxHtmlSizeKb, errors);

        CheckCommonFiles(images, audio, video, html, errors);

        var (usedImages, usedAudio, usedVideo, usedHtml) = CollectUsedFiles(true, images, audio, video, html, errors);

        var unusedImages = images.Except(usedImages);
        var unusedAudio = audio.Except(usedAudio);
        var unusedVideo = video.Except(usedVideo);
        var unusedHtml = html.Except(usedHtml);

        var unusedFiles = unusedImages.Union(unusedAudio).Union(unusedVideo).Union(unusedHtml).ToArray();

        if (unusedFiles.Length == 0)
        {
            return;
        }

        if (!PlatformManager.Instance.ConfirmExclWithWindow($"{Resources.ConfirmFilesRemoval}: {string.Join(", ", unusedFiles)}?"))
        {
            return;
        }

        foreach (var item in unusedImages)
        {
            RemoveFileByName(Images, item);
        }

        foreach (var item in unusedAudio)
        {
            RemoveFileByName(Audio, item);
        }

        foreach (var item in unusedVideo)
        {
            RemoveFileByName(Video, item);
        }

        foreach (var item in unusedHtml)
        {
            RemoveFileByName(Html, item);
        }
    }
}
