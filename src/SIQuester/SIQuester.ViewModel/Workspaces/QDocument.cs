﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notions;
using Polly;
using Polly.Retry;
using SIPackages;
using SIPackages.Containers;
using SIPackages.Core;
using SIPackages.Models;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Serializers;
using SIQuester.ViewModel.Services;
using SIQuester.ViewModel.Workspaces.Dialogs;
using SIQuester.ViewModel.Workspaces.Sidebar;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using System.Xml;
using System.Xml.Xsl;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

// TODO: this class is too heavy. It requires refactoring

/// <summary>
/// Represents a document opened inside the editor.
/// </summary>
public sealed class QDocument : WorkspaceViewModel
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };
    private const string ClipboardKey = "siqdata";

    private const string SIExtension = "siq";

    /// <summary>
    /// Maximum file size allowed by game server.
    /// </summary>
    private const int GameServerFileSizeLimitMB = 100;

    /// <summary>
    /// Maximum file size allowed by game server for packages with quality control.
    /// </summary>
    private const int GameServerFileSizeQualityLimitMB = 150;

    private const string ContentFileName = "content.xml";

    private const string ChangesFileName = "changes.json";

    /// <summary>
    /// Созданный объект
    /// </summary>
    public static object? ActivatedObject { get; set; }

    internal Lock Lock { get; }

    private bool _changed = false;

    /// <summary>
    /// Saves document under different name.
    /// </summary>
    public IAsyncCommand SaveAs { get; private set; }

    /// <summary>
    /// Copies document item.
    /// </summary>
    public ICommand Copy { get; private set; }

    /// <summary>
    /// Pastes document item.
    /// </summary>
    public ICommand Paste { get; private set; }

    public OperationsManager OperationsManager { get; } = new();

    private IItemViewModel? _activeNode = null;

    private IItemViewModel[] _activeChain = Array.Empty<IItemViewModel>();

    public MediaStorageViewModel Images { get; private set; }

    public MediaStorageViewModel Audio { get; private set; }

    public MediaStorageViewModel Video { get; private set; }

    public MediaStorageViewModel Html { get; private set; }

    public AppSettings Settings => AppSettings.Default;

    public MediaStorageViewModel? TryGetCollectionByMediaType(string mediaType) => mediaType switch
    {
        ContentTypes.Image => Images,
        ContentTypes.Audio => Audio,
        ContentTypes.Video => Video,
        ContentTypes.Html => Html,
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

    private CancellationTokenSource? _cancellation = null;

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

        foreach (var content in question.Model.GetContent())
        {
            ClearLinks(content);
        }
    }

    internal void ClearLinks(ContentItem contentItem)
    {
        if (!AppSettings.Default.RemoveLinks || OperationsManager.IsMakingUndo || !contentItem.IsRef)
        {
            return;
        }

        var contentType = contentItem.Type;
        var link = contentItem.Value;

        if (contentType != ContentTypes.Text && !HasLinksTo(link)) // Called after items removal so works properly
        {
            var collection = GetCollectionByMediaType(contentType);
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
    /// Exports package into preview image.
    /// </summary>
    public ICommand ExportPreview { get; private set; }

    /// <summary>
    /// Exports package to question database format.
    /// </summary>
    public ICommand ExportBase { get; private set; }
    
    /// <summary>
    /// Экспорт в формат Динабанка
    /// </summary>
    public ICommand ExportDinabank { get; private set; }

    /// <summary>
    /// Exports package to Steam Workshop.
    /// </summary>
    public ICommand ExportToSteam { get; }

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

    public ICommand DownloadAllExternalMedia { get; private set; }

    public ICommand Delete { get; private set; }

    public SimpleCommand NextSearchResult { get; private set; }

    public SimpleCommand PreviousSearchResult { get; private set; }

    public SimpleCommand ClearSearchText { get; private set; }

    #endregion

    private bool _isSideOpened;

    /// <summary>
    /// Is side panel opened.
    /// </summary>
    public bool IsSideOpened
    {
        get => _isSideOpened;
        set
        {
            if (_isSideOpened != value)
            {
                _isSideOpened = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Current active node.
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
                
                if (_activeItem is ContentItemsViewModel contentItems && contentItems.Owner != _activeNode)
                {
                    ActiveItem = null;
                }
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
                if (_activeItem is ContentItemsViewModel contentItems)
                {
                    contentItems.CurrentItem = null;
                }

                _activeItem = value;
                OnPropertyChanged();
            }
        }
    }

    internal DataCollection GetInternalCollection(string name) =>
        name switch
        {
            CollectionNames.ImagesStorageName => Document.Images,
            CollectionNames.AudioStorageName => Document.Audio,
            CollectionNames.VideoStorageName => Document.Video,
            CollectionNames.HtmlStorageName => Document.Html,
            _ => throw new ArgumentException($"Invalid collection name {name}", nameof(name))
        };

    public MediaStorageViewModel GetCollection(string name) =>
        name switch
        {
            CollectionNames.ImagesStorageName => Images,
            CollectionNames.AudioStorageName => Audio,
            CollectionNames.VideoStorageName => Video,
            CollectionNames.HtmlStorageName => Html,
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

    private string _filename;

    public string FileName
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

    private int _sideIndex = 0;

    public int SideIndex
    {
        get => _sideIndex;
        set
        {
            if (_sideIndex != value)
            {
                _sideIndex = value;
                OnPropertyChanged();
            }
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
        Package.Tags.CollectionChanged += Object_CollectionChanged;

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

        OperationsManager.RecordComplexChange(() => OnTypeNameChanged(question, oldValue));
    }

    private void OnTypeNameChanged(QuestionViewModel question, string oldValue)
    {
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
            if (parameter.Key == QuestionParameterNames.Question
                || parameter.Key == QuestionParameterNames.Answer
                || parameter.Key == QuestionParameterNames.AnswerType
                || parameter.Key == QuestionParameterNames.AnswerOptions)
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
            question.Parameters.InsertSorted(new StepParameterRecord(parameter.Item1, new StepParameterViewModel(question, parameter.Item2)));
        }
    }

    private void Object_PropertyValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (OperationsManager.IsMakingUndo || sender == null || e.PropertyName == null)
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
                    numberSetViewModel.Step = 0;
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

            OperationsManager.AddChange(
                new SimplePropertyValueChange
                {
                    Element = sender,
                    PropertyName = e.PropertyName,
                    Value = ext.OldValue
                });
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
                if (e.NewItems == null)
                {
                    return;
                }

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
                    else if (item is StepParameterRecord parameter)
                    {
                        AttachParameterListeners(parameter);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    return;
                }

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
                    else if (item is StepParameterRecord parameter)
                    {
                        DetachParameterListeners(parameter);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems == null || e.OldItems == null)
                {
                    return;
                }

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

    private readonly IClipboardService _clipboardService;
    private readonly ILoggerFactory _loggerFactory;

    internal QDocument(
        SIDocument document,
        StorageContextViewModel storageContextViewModel,
        IClipboardService clipboardService,
        ILoggerFactory loggerFactory)
    {
        Lock = new Lock(document.Package.Name);

        OperationsManager.Changed += OperationsManager_Changed;
        OperationsManager.Error += OperationsManager_Error;

        _clipboardService = clipboardService;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<QDocument>();

        StorageContext = storageContextViewModel;

        ImportSiq = new SimpleCommand(ImportSiq_Executed);

        Save = new AsyncCommand(Save_Executed);
        SaveAs = new AsyncCommand(SaveAs_Executed);
        SaveAsTemplate = new AsyncCommand(SaveAsTemplate_Executed);

        ExportPreview = new SimpleCommand(ExportPreview_Executed);
        ExportBase = new SimpleCommand(ExportBase_Executed);
        ExportDinabank = new SimpleCommand(ExportDinabank_Executed);
        ExportToSteam = new SimpleCommand(ExportToSteam_Executed);
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
        DownloadAllExternalMedia = new SimpleCommand(DownloadAllExternalMedia_Executed);

        Delete = new SimpleCommand(Delete_Executed);

        Copy = new SimpleCommand(Copy_Executed);
        Paste = new SimpleCommand(Paste_Executed);

        NextSearchResult = new SimpleCommand(NextSearchResult_Executed) { CanBeExecuted = false };
        PreviousSearchResult = new SimpleCommand(PreviousSearchResult_Executed) { CanBeExecuted = false };
        ClearSearchText = new SimpleCommand(ClearSearchText_Executed) { CanBeExecuted = false };

        _filename = "";
        _path = "";
        _searchText = "";

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

        Images = new MediaStorageViewModel(this, Document.Images, Resources.Images, msvmLogger, true);
        Audio = new MediaStorageViewModel(this, Document.Audio, Resources.Audio, msvmLogger);
        Video = new MediaStorageViewModel(this, Document.Video, Resources.Video, msvmLogger);
        Html = new MediaStorageViewModel(this, Document.Html, Resources.Html, msvmLogger);

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

    private ICollection<string> FillFiles(MediaStorageViewModel mediaStorage, int maxFileSize, List<WarningViewModel> warnings)
    {
        var files = new List<string>();

        foreach (var item in mediaStorage.Files)
        {
            var name = item.Model.Name;

            if (files.Contains(name))
            {
                warnings.Add(new WarningViewModel(string.Format(Resources.FileIsDuplicated, name), () => NavigateToStorageItem(mediaStorage, item)));
            }

            if (AppSettings.Default.CheckFileSize && mediaStorage.GetLength(item.Model.Name) > maxFileSize * 1024 * 1024)
            {
                warnings.Add(new WarningViewModel(string.Format(Resources.InvalidFileSize, name, maxFileSize), () => NavigateToStorageItem(mediaStorage, item)));
            }

            files.Add(name);
        }

        return files;
    }

    internal void NavigateToStorageItem(MediaStorageViewModel mediaStorage, MediaItemViewModel? item)
    {
        IsSideOpened = true;
        SideIndex = mediaStorage == Images ? 2 : (mediaStorage == Audio ? 3 : (mediaStorage == Video ? 4 : 5));
        mediaStorage.CurrentFile = item;
    }

    /// <summary>
    /// Checks missing and unused files in document.
    /// </summary>
    internal async Task<(IEnumerable<WarningViewModel>, string)> CheckLinksAsync()
    {
        var warnings = new List<WarningViewModel>();
        var errors = new List<string>();
        var recommendedSize = Quality.FileSizeMb;

        var images = FillFiles(Images, recommendedSize[CollectionNames.ImagesStorageName], warnings);
        var audio = FillFiles(Audio, recommendedSize[CollectionNames.AudioStorageName], warnings);
        var video = FillFiles(Video, recommendedSize[CollectionNames.VideoStorageName], warnings);
        var html = FillFiles(Html, recommendedSize[CollectionNames.HtmlStorageName], warnings);

        CheckCommonFiles(images, audio, video, html, errors);

        var (usedImages, usedAudio, usedVideo, usedHtml) = await CollectUsedFilesAsync(images, warnings);

        foreach (var item in images.Except(usedImages))
        {
            warnings.Add(
                new WarningViewModel(string.Format(Resources.UnusedFile, item),
                () => NavigateToStorageItem(Images, Images.Files.FirstOrDefault(f => f.Model.Name == item))));
        }

        foreach (var item in audio.Except(usedAudio))
        {
            warnings.Add(
                new WarningViewModel(string.Format(Resources.UnusedFile, item),
                () => NavigateToStorageItem(Audio, Images.Files.FirstOrDefault(f => f.Model.Name == item))));
        }

        foreach (var item in video.Except(usedVideo))
        {
            warnings.Add(
                new WarningViewModel(string.Format(Resources.UnusedFile, item),
                () => NavigateToStorageItem(Video, Images.Files.FirstOrDefault(f => f.Model.Name == item))));
        }

        foreach (var item in html.Except(usedHtml))
        {
            warnings.Add(
                new WarningViewModel(string.Format(Resources.UnusedFile, item),
                () => NavigateToStorageItem(Html, Images.Files.FirstOrDefault(f => f.Model.Name == item))));
        }

        return (warnings, string.Join(Environment.NewLine, errors));
    }

    private async Task<(ICollection<string> usedImages,
        ICollection<string> usedAudio,
        ICollection<string> usedVideo,
        ICollection<string> usedHtml)>
        CollectUsedFilesAsync(
        ICollection<string> images,
        List<WarningViewModel> warnings)
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
                warnings.Add(new WarningViewModel(
                    string.Format(Resources.MissingLogoFile, logoItem.Value),
                    () => ActiveNode = Package));
            }
        }

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItemViewModel in question.GetContent())
                    {
                        var owner = contentItemViewModel.Owner;

                        if (owner == null)
                        {
                            continue;
                        }

                        var contentItem = contentItemViewModel.Model;
                        HashSet<string> usedFiles;

                        void navigateToItem()
                        {
                            Navigate.Execute(question);
                            ActiveItem = owner;
                            owner.CurrentItem = contentItemViewModel;
                        }

                        switch (contentItem.Type)
                        {
                            case ContentTypes.Image:
                                usedFiles = usedImages;
                                break;

                            case ContentTypes.Audio:
                                usedFiles = usedAudio;
                                break;

                            case ContentTypes.Video:
                                usedFiles = usedVideo;
                                break;

                            case ContentTypes.Html:
                                usedFiles = usedHtml;
                                break;

                            default:
                                continue;
                        }

                        var collection = TryGetCollectionByMediaType(contentItem.Type);

                        if (collection != null && collection.Files.Any(f => f.Model.Name == contentItem.Value))
                        {
                            usedFiles.Add(contentItem.Value);
                        }
                        else if (!contentItem.IsRef)
                        {
                            try
                            {
                                var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, contentItem.Value));

                                if (!response.IsSuccessStatusCode)
                                {
                                    warnings.Add(new WarningViewModel(
                                        $"{Resources.MissingLink} \"{contentItem.Value}\" ({await response.Content.ReadAsStringAsync()})",
                                        navigateToItem));
                                }
                            }
                            catch (HttpRequestException exc)
                            {
                                warnings.Add(new WarningViewModel(
                                    $"{Resources.MissingLink} \"{contentItem.Value}\" ({exc.Message})",
                                    navigateToItem));
                            }

                            continue; // External file
                        }
                        else
                        {
                            warnings.Add(new WarningViewModel(
                                $"{Resources.MissingFile} \"{contentItem.Value}\"",
                                navigateToItem));
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

    internal void Copy_Executed(object? arg)
    {
        if (_activeNode == null)
        {
            return;
        }

        try
        {
            var itemData = new InfoOwnerData(this, _activeNode);
            _clipboardService.SetData(ClipboardKey, itemData);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    internal void Paste_Executed(object? arg)
    {
        if (_activeNode == null)
        {
            return;
        }

        if (!_clipboardService.ContainsData(ClipboardKey))
        {
            return;
        }

        try
        {
            using var change = OperationsManager.BeginComplexChange();

            var itemData = (InfoOwnerData)_clipboardService.GetData(ClipboardKey);
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
                                if (item.Type != ContentTypes.Image && item.Type != ContentTypes.Audio && item.Type != ContentTypes.Video && item.Type != ContentTypes.Html)
                                {
                                    continue;
                                }

                                await ImportContentItemAsync(doc, item, contentImportTable);
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

        var media = doc.TryGetMedia(contentItem);

        if (!media.HasValue || !media.Value.HasStream)
        {
            return;
        }

        var mediaName = contentItem.Value;

        if (contentImportTable.TryGetValue(mediaName, out var newName))
        {
            contentItem.Value = newName;
            return;
        }

        var fileName = FileHelper.GenerateUniqueFileName(mediaName, name => newCollection.Files.Any(f => f.Model.Name == name));
        contentImportTable[mediaName] = fileName;

        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            AppSettings.ProductName,
            AppSettings.TempMediaFolderName,
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempPath);

        var tempFile = System.IO.Path.Combine(tempPath, fileName);

        using (var fileStream = File.Create(tempFile))
        using (var mediaStream = media.Value.Stream!)
        {
            await mediaStream.CopyToAsync(fileStream);
        }

        newCollection.AddFile(tempFile);
        contentItem.Value = fileName;
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
            if (AppSettings.Default.AskToSetTagsOnSave && Package.Tags.Count == 0)
            {
                Package.AddTags.Execute(null);
            }

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

    private void ExportPreview_Executed(object? arg)
    {
        try
        {
            PlatformManager.Instance.CreatePreview(Document);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void ExportBase_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.Db);

    private void ExportDinabank_Executed(object? arg) => Dialog = new ExportViewModel(this, ExportFormats.Dinabank);

    private async void ExportToSteam_Executed(object? arg)
    {
        try
        {
            var validationResult = ValidateForSteam();

            if (validationResult != null)
            {
                PlatformManager.Instance.ShowExclamationMessage(validationResult);
                return;
            }

            await Save.ExecuteAsync(null);
            Dialog = new ExportToSteamViewModel(this);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private string? ValidateForSteam()
    {
        if (!Package.HasQualityControl)
        {
            return Resources.ExportToSteamQualityControlDisabled;
        }

        var hasQuestions = false;

        foreach (var round in Package.Model.Rounds)
        {
            if (round.Name.Length == 0)
            {
                return $"{Resources.ExportToSteamEmptyRound}";
            }

            foreach (var theme in round.Themes)
            {
                if (theme.Name.Length == 0)
                {
                    return $"{Resources.Round} {round.Name}: {Resources.ExportToSteamEmptyTheme}";
                }

                foreach (var question in theme.Questions)
                {
                    if (question.Price != Question.InvalidPrice)
                    {
                        hasQuestions = true;
                    }

                    var questionText = question.GetText();

                    var emptyNormal = question.Price == Question.InvalidPrice || question.TypeName == QuestionTypes.SecretNoQuestion;

                    var emptyQuestion = !emptyNormal
                        && (questionText == "" || questionText == Resources.Question)
                        && !question.HasMediaContent();

                    if (emptyQuestion)
                    {
                        return $"{Resources.Round} {round.Name}: {theme.Name}, {question.Price}: {Resources.ExportToSteamEmptyQuestion}";
                    }

                    var noAnswer = !emptyNormal && (question.Right.Count == 0 || question.Right.Count == 1 && string.IsNullOrWhiteSpace(question.Right[0]));

                    if (noAnswer)
                    {
                        return $"{Resources.Round} {round.Name}: {theme.Name}, {question.Price}: {Resources.ExportToSteamNoAnswer}";
                    }
                }
            }
        }

        if (!hasQuestions)
        {
            return Resources.ExportToSteamNoQuestions;
        }

        return null;
    }

    private async void ExportTable_Executed(object? arg)
    {
        string? filename = FileName.Replace(".", "-");

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
                var baseFolder = System.IO.Path.GetDirectoryName(filename) ?? throw new InvalidOperationException($"Wrong filename {filename} for exporting YAML");

                using (var textWriter = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    YamlSerializer.SerializePackage(textWriter, Document.Package);
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

    private async void Navigate_Executed(object? arg)
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

        var expanded = false;

        while (parent != null) // Expanding to leaf
        {
            expanded |= !parent.IsExpanded;
            parent.IsExpanded = true;
            parent = parent.Owner;
        }

        ActiveItem = null;

        if (expanded)
        {
            await Task.Delay(400);
        }

        infoOwner.IsSelected = true;
        ActiveNode = infoOwner;
    }

    private static readonly RetryPolicy Retry = Policy
        .Handle<IOException>(ex => ex.Message.Contains("The process cannot access the file because it is being used by another process"))
        .WaitAndRetry(3, retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt));

    internal ValueTask SaveInternalAsync() =>
         Lock.WithLockAsync(async () =>
         {
             // 1. Saving at temporary path to validate saved file first
             var tempPath = FileHelper.GenerateUniqueFilePath(System.IO.Path.ChangeExtension(_path, "tmp"));

             FileStream tempStream;

             try
             {
                 tempStream = File.Open(tempPath, FileMode.Create, FileAccess.ReadWrite);
             }
             catch (FileNotFoundException ex)
             {
                 throw new Exception(Resources.CannotCreateTemporaryFile, ex);
             }

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

             File.SetAttributes(tempPath, File.GetAttributes(tempPath) | FileAttributes.Hidden);

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
                     Retry.Execute(() => File.Replace(tempPath, _path, null)); // It is possible to provide backup file on save here
                 }
                 catch (IOException exc) when (exc.Message.Contains("The process cannot access the file because it is being used by another process"))
                 {
                     _logger.LogWarning(exc, "SaveInternalAsync error. Switching to old saving method: {error}", exc.Message);

                     // Fallback to old unsafe method
                     File.Copy(tempPath, _path, true); // File.Copy is not atomic and could corrupt target file
                     File.Delete(tempPath);
                 }
                 catch (UnauthorizedAccessException exc)
                 {
                     _logger.LogWarning(exc, "SaveInternalAsync error. Switching to old saving method: {error}", exc.Message);

                     // Fallback to old unsafe method
                     File.Copy(tempPath, _path, true); // File.Copy is not atomic and could corrupt target file
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
        if (!AppSettings.Default.CheckFileSize && !Document.Package.HasQualityControl)
        {
            return null;
        }

        var sizeLimit = Document.Package.HasQualityControl ? GameServerFileSizeQualityLimitMB : GameServerFileSizeLimitMB;

        if (!string.IsNullOrEmpty(_path) && new FileInfo(_path).Length > sizeLimit * 1024 * 1024)
        {
            return string.Format(Resources.FileSizeLimitExceed, sizeLimit);
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

    internal Task SaveAs_Executed(object? arg) => SaveAsAsync();

    private async Task SaveAsAsync()
    {
        try
        {
            string? filename = Document.Package.Name;

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
        SearchResults = new SearchResults { Query = query };
        var package = Package;

        if (package.Model.ContainsInfoOwner(query))
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
            if (round.Model.ContainsInfoOwner(query))
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
                if (theme.Model.ContainsInfoOwner(query))
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
                    foreach (var item in quest.Model.GetContent())
                    {
                        if (item.Type == ContentTypes.Text)
                        {
                            item.Value = item.Value.Wikify();
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
                theme.Model.Name = theme.Model.Name.ToUpper().ClearPoints();
                theme.OwnerRound = null;
            });

            for (var i = 0; i < 3; i++)
            {
                var round = new Round { Name = string.Format(Resources.RoundNameTemplate, Package.Rounds.Count + 1), Type = RoundTypes.Standart };
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
                            var question = PackageItemsHelper.CreateQuestion(100 * (i + 1) * (k + 1));

                            questionViewModel = new QuestionViewModel(question);
                            themeViewModel.Questions.Add(questionViewModel);
                        }
                        else
                        {
                            questionViewModel = themeViewModel.Questions[k];
                            questionViewModel.Model.Price = (100 * (i + 1) * (k + 1));
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
                    var question = PackageItemsHelper.CreateQuestion(0);

                    questionViewModel = new QuestionViewModel(question);
                    themeViewModel.Questions.Add(questionViewModel);
                }
                else
                {
                    questionViewModel = themeViewModel.Questions[0];
                    questionViewModel.Model.Price = 0;
                }

                if (questionViewModel.Right.Count > 0)
                {
                    questionViewModel.Right[0] = questionViewModel.Right[0].ClearPoints().GrowFirstLetter();
                }
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

                    if (quest.Parameters != null)
                    {
                        foreach (var parameter in quest.Parameters)
                        {
                            if (parameter.Value.ContentValue != null)
                            {
                                foreach (var contentItem in parameter.Value.ContentValue)
                                {
                                    if (contentItem.Type == ContentTypes.Text)
                                    {
                                        contentItem.Model.Value = contentItem.Model.Value.Wikify().ClearPoints().GrowFirstLetter();
                                    }
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
        try
        {
            using var change = OperationsManager.BeginComplexChange();
            var allq = new List<QuestionViewModel>();

            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    allq.AddRange(theme.Questions);

                    theme.Questions.ClearOneByOne();
                }

                round.Themes.ClearOneByOne();
            }

            Package.Rounds.ClearOneByOne();

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
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    #endregion

    private void SelectThemes_Executed(object? arg)
    {
        var selectThemesViewModel = new SelectThemesViewModel(
            this,
            PlatformManager.Instance.ServiceProvider.GetRequiredService<IDocumentViewModelFactory>());

        selectThemesViewModel.NewItem += OnNewItem;
        Dialog = selectThemesViewModel;
    }

    private void PlayQuestion_Executed(object? arg)
    {
        if (arg is not QuestionViewModel questionViewModel)
        {
            return;
        }

        Dialog = new QuestionPlayViewModel(questionViewModel, this);
    }

    internal bool CheckPackageQuality()
    {
        var errors = new List<string>();

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItem in question.Model.GetContent())
                    {
                        if (contentItem.Type != ContentTypes.Text)
                        {
                            if (!contentItem.IsRef)
                            {
                                errors.Add($"{round.Model.Name}:{theme.Model.Name}:{question.Model.Price}:{contentItem.Value}: {Resources.ExternalLinksAreForbidden}");
                            }
                            else
                            {
                                var collectionName = CollectionNames.TryGetCollectionName(contentItem.Type);

                                if (collectionName != null)
                                {
                                    var media = Wrap(contentItem);
                                    var maxFileSize = Quality.FileSizeMb[collectionName];

                                    if (media.StreamLength > maxFileSize * 1024 * 1024)
                                    {
                                        var errorMessage = string.Format(Resources.InvalidFileSize, contentItem.Value, maxFileSize).LeaveFirst(2000);
                                        errors.Add($"{round.Model.Name}:{theme.Model.Name}:{question.Model.Price}: {errorMessage}");
                                    }

                                    var extensions = Quality.FileExtensions[collectionName];
                                    var extension = System.IO.Path.GetExtension(contentItem.Value)?.ToLowerInvariant();

                                    if (!extensions.Contains(extension))
                                    {
                                        var errorMessage = string.Format(
                                            Resources.InvalidFileExtension,
                                            contentItem.Value,
                                            extension,
                                            string.Join(", ", extensions));

                                        errors.Add($"{round.Model.Name}:{theme.Model.Name}:{question.Model.Price}: {errorMessage}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            PlatformManager.Instance.ShowExclamationMessage(
                Resources.CannotEnableQuality + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, errors));

            IsSideOpened = true;
            SideIndex = 6;
            return false;
        }

        return true;
    }

    private async void ExpandAll_Executed(object? arg)
    {
        var expand = Convert.ToBoolean(arg);

        Package.IsExpanded = expand;

        var allThemes = new List<ThemeViewModel>();

        foreach (var round in Package.Rounds)
        {
            round.IsExpanded = expand;
            allThemes.AddRange(round.Themes);
        }

        foreach (var theme in allThemes)
        {
            theme.IsExpanded = expand;

            if (expand)
            {
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

    private async void DownloadAllExternalMedia_Executed(object? arg)
    {
        var tempMediaFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName, Guid.NewGuid().ToString());
        var directoryCreated = false;
        var downloadCounter = 0;

        var errors = new List<string>();

        try
        {
            using var change = OperationsManager.BeginComplexChange();

            foreach (var round in Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    foreach (var question in theme.Questions)
                    {
                        foreach (var content in question.Model.GetContent())
                        {
                            if (content.Type == ContentTypes.Text || content.IsRef)
                            {
                                continue;
                            }

                            var collection = TryGetCollectionByMediaType(content.Type);
                            var fileName = System.IO.Path.GetFileName(content.Value);

                            if (collection == null)
                            {
                                continue;
                            }

                            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(System.IO.Path.GetExtension(fileName)))
                            {
                                fileName = $"{Guid.NewGuid()}.{GetDefaultExtensionByContentType(content.Type)}";
                            }

                            if (!directoryCreated)
                            {
                                Directory.CreateDirectory(tempMediaFolder);
                                directoryCreated = true;
                            }

                            var link = content.Value;
                            var tmpFile = System.IO.Path.Combine(tempMediaFolder, fileName);

                            try
                            {
                                using var stream = await HttpClient.GetStreamAsync(link);
                                using var fs = File.Create(tmpFile);
                                await stream.CopyToAsync(fs);
                            }
                            catch (Exception exc)
                            {
                                errors.Add($"{round.Model.Name}:{theme.Model.Name}:{question.Model.Price}: {exc.Message}");
                                continue;
                            }

                            var item = collection.AddFile(tmpFile);
                            content.Value = item.Name;
                            content.IsRef = true;

                            question.Info.Sources.Add(link);

                            downloadCounter++;
                        }
                    }
                }
            }

            change.Commit();

            if (errors.Count > 0)
            {
                PlatformManager.Instance.ShowExclamationMessage(string.Join(Environment.NewLine, errors));
            }

            PlatformManager.Instance.Inform(string.Format(Resources.FilesDownloaded, downloadCounter));
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private static string GetDefaultExtensionByContentType(string type) => type switch
    {
        ContentTypes.Image => ".jpg",
        ContentTypes.Audio => ".mp3",
        ContentTypes.Video => ".mp4",
        ContentTypes.Html => ".html",
        _ => ".dat"
    };

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

                        var mediaName = contentItem.Value;
                        var mediaStorage = GetCollectionByMediaType(contentItem.Type);

                        if (mediaStorage.Files.Any(f => f.Model.Name == mediaName))
                        {
                            continue;
                        }

                        var mediaFileName = System.IO.Path.Combine(folder, mediaName);

                        if (!File.Exists(mediaFileName))
                        {
                            mediaFileName = System.IO.Path.Combine(folder, mediaStorage.Name, mediaName);

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
                Package.Model.Publisher = package.Publisher;
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
                                questionViewModel.Model.TypeName = question.TypeName;

                                questionViewModel.Right.Merge(question.Right);
                                questionViewModel.Wrong.Merge(question.Wrong);

                                if (question.Parameters != null && questionViewModel.Parameters != null)
                                {
                                    questionViewModel.Parameters.Merge(
                                        question.Parameters.ToList(),
                                        p => new StepParameterRecord(p.Key, new StepParameterViewModel(questionViewModel, p.Value)));
                                }
                            });
                    });
            });
    }

    /// <summary>
    /// Confirms and removes unused files from document.
    /// </summary>
    internal async Task RemoveUnusedFilesAsync()
    {
        var warnings = new List<WarningViewModel>();
        var errors = new List<string>();
        var recommendedSize = Quality.FileSizeMb;

        var images = FillFiles(Images, recommendedSize[CollectionNames.ImagesStorageName], warnings);
        var audio = FillFiles(Audio, recommendedSize[CollectionNames.AudioStorageName], warnings);
        var video = FillFiles(Video, recommendedSize[CollectionNames.VideoStorageName], warnings);
        var html = FillFiles(Html, recommendedSize[CollectionNames.HtmlStorageName], warnings);

        CheckCommonFiles(images, audio, video, html, errors);

        var (usedImages, usedAudio, usedVideo, usedHtml) = await CollectUsedFilesAsync(images, warnings);

        var unusedImages = images.Except(usedImages);
        var unusedAudio = audio.Except(usedAudio);
        var unusedVideo = video.Except(usedVideo);
        var unusedHtml = html.Except(usedHtml);

        var unusedFiles = new StringBuilder();

        foreach (var file in unusedImages)
        {
            if (unusedFiles.Length > 0)
            {
                unusedFiles.Append(", ");
            }

            unusedFiles.Append($"{Resources.Image}: {file}");
        }

        foreach (var file in unusedAudio)
        {
            if (unusedFiles.Length > 0)
            {
                unusedFiles.Append(", ");
            }

            unusedFiles.Append($"{Resources.Audio}: {file}");
        }

        foreach (var file in unusedVideo)
        {
            if (unusedFiles.Length > 0)
            {
                unusedFiles.Append(", ");
            }

            unusedFiles.Append($"{Resources.Video}: {file}");
        }

        foreach (var file in unusedHtml)
        {
            if (unusedFiles.Length > 0)
            {
                unusedFiles.Append(", ");
            }

            unusedFiles.Append($"{Resources.Html}: {file}");
        }

        if (unusedFiles.Length == 0)
        {
            return;
        }

        if (!PlatformManager.Instance.ConfirmExclamationWithWindow($"{Resources.ConfirmFilesRemoval}: {string.Join(", ", unusedFiles)}?"))
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

    internal TimeSpan GetDurationByContentType(string contentType)
    {
        if (contentType == ContentTypes.Image && Settings.UseImageDuration)
        {
            return TimeSpan.FromSeconds(Settings.ImageDurationSeconds);
        }

        return TimeSpan.Zero;
    }

    internal void RenameContentReference(string mediaType, string oldValue, string newValue)
    {
        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var contentItem in question.Model.GetContent())
                    {
                        if (contentItem.Type == mediaType && contentItem.Value == oldValue)
                        {
                            contentItem.Value = newValue;
                        }
                    }
                }
            }
        }
    }
}
