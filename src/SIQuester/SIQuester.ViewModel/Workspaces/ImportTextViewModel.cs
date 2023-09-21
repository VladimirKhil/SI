using Lingware.Spard.Expressions;
using Microsoft.Extensions.Logging;
using QTxtConverter;
using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

// TODO: this class is too heavy. It requires refactoring

/// <summary>
/// Provides a view model for text file import.
/// </summary>
public sealed class ImportTextViewModel : WorkspaceViewModel
{
    private Task? _task;

    private readonly string _header = Resources.TextImport;

    public override string Header => _header;

    public enum UIState
    {
        Initial,
        ImportFile,
        Split,
        Parse,
    }

    private UIState _state = UIState.Initial;

    public UIState State
    {
        get => _state;
        set { if (_state != value) { _state = value; OnPropertyChanged(); } }
    }

    public ICommand SelectFile { get; private set; }

    public ICommand Run { get; private set; }

    public string? FileName => _textSource?.FileName;


    private string _importText = "";

    /// <summary>
    /// Text to import.
    /// </summary>
    public string ImportText
    {
        get => _importText;
        set { _importText = value; OnPropertyChanged(); }
    }

    private string _text = "";
    
    private ITextSource? _textSource;

    /// <summary>
    /// Input text.
    /// </summary>
    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    private string[] _fragments = Array.Empty<string>();

    /// <summary>
    /// Input fragments.
    /// </summary>
    public string[] Fragments
    {
        get => _fragments;
        set
        {
            _fragments = value;
            OnPropertyChanged();
        }
    }

    private int _currentFragmentIndex;

    /// <summary>
    /// Current input fragment index.
    /// </summary>
    public int CurrentFragmentIndex
    {
        get => _currentFragmentIndex;
        set { _currentFragmentIndex = value; OnPropertyChanged(); }
    }

    public string CurrentFragmentText => _currentFragmentIndex > -1 && _currentFragmentIndex < _fragments.Length ? _fragments[_currentFragmentIndex] : "";

    private string _goodText = "";

    public string GoodText
    {
        get => _goodText;
        set { _goodText = value; OnPropertyChanged(); }
    }

    private string _badText = "";

    public string BadText
    {
        get => _badText;
        set { _badText = value; OnPropertyChanged(); }
    }

    private bool _free = false;

    public bool Free
    {
        get => _free;
        set
        {
            _free = value;
            EnableTemplates(_free);
            OnPropertyChanged();
        }
    }

    private void EnableTemplates(bool free)
    {
        foreach (var item in Templates)
        {
            item.Enabled = free;
        }
    }

    private bool _canGo = false;

    public bool CanGo
    {
        set
        {
            _canGo = value;
            OnReadyChanged();
        }
    }

    private void OnReadyChanged()
    {
        OnPropertyChanged(nameof(Ready));
        _go.CanBeExecuted = Ready;
    }

    /// <summary>
    /// Можно ли начать преобразование
    /// </summary>
    public bool Ready => _canGo && (_stage == Stage.SplitResolve ||
        (_template != null
        && Templates.All(t => t.Transform != null && t.Transform.Length > 6 ||
        _template.StandartLogic && t.NonStandartOnly)));

    private bool _standartLogic = true;

    public bool StandartLogic
    {
        get => _standartLogic;
        set
        {
            if (_standartLogic != value)
            {
                _standartLogic = value;

                if (value)
                {
                    Templates.Remove(_separatorTemplate);
                    Templates.Remove(_answerTemplate);
                }
                else
                {
                    Templates.Add(_separatorTemplate);
                    Templates.Add(_answerTemplate);
                }

                OnPropertyChanged();
            }
        }
    }

    private string _goText = "";

    public string GoText
    {
        get => _goText;
        set { _goText = value; OnPropertyChanged(); }
    }

    private bool _isEditorOpened;

    public bool IsEditorOpened
    {
        get => _isEditorOpened;
        set
        {
            if (_isEditorOpened != value)
            {
                _isEditorOpened = value;
                OnPropertyChanged();
            }
        }
    }

    private int _position = 0;
    private int _progress = 0;

    public int Progress
    {
        get => _progress;
        set { if (_progress != value) { _progress = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Шаблоны распознавания
    /// </summary>
    public ObservableCollection<SpardTemplateViewModel> Templates { get; } = new();

    private readonly SpardTemplateViewModel _packageTemplate;
    private readonly SpardTemplateViewModel _roundTemplate;
    private readonly SpardTemplateViewModel _themeTemplate;
    private readonly SpardTemplateViewModel _questTemplate;
    private readonly SpardTemplateViewModel _separatorTemplate;
    private readonly SpardTemplateViewModel _answerTemplate;

    #region Commands

    private readonly SimpleCommand _sns;
    private readonly SimpleCommand _auto;
    private readonly SimpleCommand _go;
    private readonly SimpleCommand _skip;

    public ICommand Sns => _sns;

    public ICommand Auto => _auto;

    public ICommand Go => _go;

    public ICommand Skip => _skip;

    #endregion

    private readonly CancellationTokenSource _tokenSource = new();
    private readonly TaskScheduler _scheduler;

    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly AppOptions _appOptions;
    private string _badTextCopy = "";

    private readonly QConverter _converter = new();

    private SIDocument? _existing = null;
    private bool _automaticTextImport = false;

    private SIPart[][]? _parts = null;
    private SITemplate? _template = null;

    private readonly object _sync = new();

    private static string BadSourceBackColor = "#FFF5DEB3";

    /// <summary>
    /// Parser state machine stage.
    /// </summary>
    private enum Stage
    {
        /// <summary>
        /// Input text review.
        /// </summary>
        Review,

        /// <summary>
        /// Splitting text into themes and questions.
        /// </summary>
        Splitting,
        
        SplitResolve,
        Automation,
        Begin,
        Reading,
        ReadingResolve,
        None
    };

    private Stage _stage = Stage.Review;

    private SplitErrorEventArgs? _parseError = null;
    private ReadErrorEventArgs? _readError = null;

    private int _badLength = 0;

    private bool _fileChanged = false;

    private Dictionary<string, EditAlias> Aliases { get; } = new();

    private string _info = "";

    public string Info
    {
        get => _info;
        set { _info = value; OnPropertyChanged(); }
    }

    private string _problem = "";

    public string Problem
    {
        get => _problem;
        set { _problem = value; OnPropertyChanged(); }
    }

    private bool _canChangeStandart = false;

    public bool CanChangeStandart
    {
        get => _canChangeStandart;
        set
        {
            if (_canChangeStandart != value)
            {
                _canChangeStandart = value;
                _auto.CanBeExecuted = _sns.CanBeExecuted = value;
                OnPropertyChanged();
            }
        }
    }

    private string _skipToolTip = "";

    public string SkipToolTip
    {
        get => _skipToolTip;
        set { _skipToolTip = value; OnPropertyChanged(); }
    }

    public event Action<int, int, string?, bool>? HighlightText;

    private readonly ILoggerFactory _loggerFactory;

    private Encoding _textEncoding = Encoding.UTF8;

    public Encoding TextEncoding
    {
        get => _textEncoding;
        set
        {
            if (_textEncoding != value)
            {
                _textEncoding = value;
                OnPropertyChanged();
                ReloadImportText();
            }
        }
    }

    public Encoding[] Encodings { get; } = new Encoding[] { Encoding.UTF8, Encoding.GetEncoding(1251) };

    static ImportTextViewModel() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// Initializes a new instance of <see cref="ImportTextViewModel" /> class.
    /// </summary>
    /// <param name="storageContextViewModel">Well-known SIStorage facets holder.</param>
    /// <param name="appOptions">Application options.</param>
    /// <param name="clipboardService">Clipboard access service.</param>
    /// <param name="loggerFactory">Factory to create loggers.</param>
    public ImportTextViewModel(StorageContextViewModel storageContextViewModel, AppOptions appOptions, IClipboardService clipboardService, ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _appOptions = appOptions;
        _loggerFactory = loggerFactory;
        _scheduler = TaskScheduler.FromCurrentSynchronizationContext();

        var trashAlias = new EditAlias(Resources.Trash, "#FFD3D3D3");

        _packageTemplate = new SpardTemplateViewModel(SIPackages.Properties.Resources.Package, clipboardService);
        _packageTemplate.Aliases["PName"] = new EditAlias(SIPackages.Properties.Resources.Package, "#FFDA70D6");
        _packageTemplate.Aliases["Some"] = trashAlias;
        Templates.Add(_packageTemplate);

        _roundTemplate = new SpardTemplateViewModel(SIPackages.Properties.Resources.Round, clipboardService);
        _roundTemplate.Aliases["RName"] = new EditAlias(SIPackages.Properties.Resources.Round, "#FFFFFFE0");
        _roundTemplate.Aliases["Some"] = trashAlias;
        Templates.Add(_roundTemplate);

        _themeTemplate = new SpardTemplateViewModel(SIPackages.Properties.Resources.Theme, clipboardService);
        _themeTemplate.Aliases["TName"] = new EditAlias(SIPackages.Properties.Resources.Theme, "#FFF5DEB3");
        _themeTemplate.Aliases["TAuthor"] = new EditAlias(Resources.Author, "#FF800000");
        _themeTemplate.Aliases["TComment"] = new EditAlias(Resources.Comment, "#FFFFA07A");
        _themeTemplate.Aliases["Some"] = trashAlias;
        Templates.Add(_themeTemplate);

        _questTemplate = new SpardTemplateViewModel(Resources.Question, clipboardService);
        _questTemplate.Aliases["Number"] = new EditAlias(Resources.Number, "#FF87CEEB");
        _questTemplate.Aliases["QText"] = new EditAlias(Resources.Question, "#FF98FB98");
        _questTemplate.Aliases["Answer"] = new EditAlias(Resources.Answer, "#FFFFFF00");
        _questTemplate.Aliases["QAuthor"] = new EditAlias(Resources.Author, "#FFDAA520");
        _questTemplate.Aliases["QComment"] = new EditAlias(Resources.Comment, "#FF00FFFF");
        _questTemplate.Aliases["QSource"] = new EditAlias(Resources.Source, "#FFD2691E");
        _questTemplate.Aliases["Some"] = trashAlias;
        Templates.Add(_questTemplate);

        _separatorTemplate = new SpardTemplateViewModel(Resources.Separator, clipboardService) { NonStandartOnly = true };
        _separatorTemplate.Aliases["Some"] = trashAlias;            

        _answerTemplate = new SpardTemplateViewModel(Resources.Answer, clipboardService) { NonStandartOnly = true };

        foreach (var item in _questTemplate.Aliases)
        {
            _answerTemplate.Aliases[item.Key] = item.Value;
        }

        foreach (var item in Templates)
        {
            item.PropertyChanged += Item_PropertyChanged;

            foreach (var alias in item.Aliases)
            {
                if (!Aliases.ContainsKey(alias.Key))
                {
                    Aliases[alias.Key] = alias.Value;
                }
            }
        }

        _sns = new SimpleCommand(Sns_Executed) { CanBeExecuted = false };
        _auto = new SimpleCommand(Auto_Executed) { CanBeExecuted = false };
        _go = new SimpleCommand(Go_Executed);
        _skip = new SimpleCommand(Skip_Executed) { CanBeExecuted = false };

        SelectFile = new SimpleCommand(SelectFile_Executed);
        Run = new SimpleCommand(Run_Executed);
        CancelImport = new SimpleCommand(CancelImport_Executed);
        ApproveImport = new SimpleCommand(ApproveImport_Executed);
        ApproveImportAndStart = new SimpleCommand(ApproveImportAndStart_Executed);

        _automaticTextImport = AppSettings.Default.AutomaticTextImport;

        _converter.ParseError += QTxtConverter_ParseError;
        _converter.ReadError += QTxtConverter_ReadError;
        _converter.Progress += QTxtConverter_Progress;

        Free = false;
        CanChangeStandart = false;
        CanGo = true;
    }

    private void SelectFile_Executed(object? arg)
    {
        var sourceFile = PlatformManager.Instance.ShowImportUI("txt", Resources.TxtFilesFilter);

        if (sourceFile == null)
        {
            return;
        }

        Import(new FileTextSource(sourceFile));
    }

    internal void Import(ITextSource textSource)
    {
        _textSource = textSource;
        OnPropertyChanged(nameof(FileName));
        ReloadImportText();
        State = UIState.ImportFile;
    }

    private void ReloadImportText() => ImportText = _textSource?.GetText(TextEncoding) ?? "";

    public ICommand CancelImport { get; private set; }

    public ICommand ApproveImport { get; private set; }

    public ICommand ApproveImportAndStart { get; private set; }

    private void CancelImport_Executed(object? arg)
    {
        State = UIState.Initial;
        _textSource?.Dispose();
    }

    private void ApproveImport_Executed(object? arg)
    {
        CommitImport();
        State = UIState.Initial;
    }

    private void CommitImport()
    {
        Text = ImportText;
        _textSource?.Dispose();
    }

    private void ApproveImportAndStart_Executed(object? arg)
    {
        CommitImport();
        Run_Executed(arg);
    }

    private void Run_Executed(object? arg)
    {
        State = UIState.Split;
        _stage = Stage.Splitting;
        Task.Factory.StartNew(Split, _tokenSource.Token);
    }

    private void Sns_Executed(object? arg) => SetTemplate(QConverter.GetSnsTemplates(_parts, _standartLogic));

    private async void Auto_Executed(object? arg)
    {
        if (_stage == Stage.Automation)
        {
            _tokenSource.Cancel();
            return;
        }

        Free = false;
        CanGo = false;
        IsEditorOpened = false;
        CanChangeStandart = false;

        _stage = Stage.Automation;

        try
        {
            await Task.Run(Autogenerate, _tokenSource.Token);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void Autogenerate()
    {
        SetTemplate(_converter.GetGeneratedTemplates(_parts, _standartLogic));
        CanGo = true;
        Free = true;
        _stage = Stage.Begin;
        CanChangeStandart = true;
        Progress = 0;
    }

    private void AnalyzeFinished(Task<Tuple<bool, int>> task)
    {
        if (task.IsFaulted)
        {
            OnError(task.Exception.InnerException);
        }
        else
        {
            var themesNum = task.Result.Item2;
            if (task.Result.Item1)
            {
                if (!task.IsCanceled)
                {
                    PlatformManager.Instance.Inform($"{Resources.Success} {themesNum}.");
                    OnNewItem(new QDocument(_existing, _storageContextViewModel, _loggerFactory) { FileName = _existing.Package.Name });
                }
            }
        }

        _task = null;
        OnClosed();
    }

    private Tuple<bool, int> Analyze()
    {
        var result = _converter.ReadFile(
            _parts,
            _template,
            ref _existing,
            false,
            string.IsNullOrEmpty(FileName) ? Resources.Untitled : Path.GetFileNameWithoutExtension(FileName),
            Resources.Empty,
            Resources.ThemesCollection,
            out int themesNum);

        if (_existing != null && _appOptions.UpgradeNewPackages)
        {
            _existing.Upgrade();
        }

        return Tuple.Create(result, themesNum);
    }

    private void Go_Executed(object? arg)
    {
        switch (_stage)
        {
            case Stage.Begin:
                GoText = Resources.Futher;
                CanGo = false;
                CanChangeStandart = false;

                _template.PackageTemplate[0] = _packageTemplate.Transform;
                _template.RoundTemplate[0] = _roundTemplate.Transform;
                _template.ThemeTemplate[0] = _themeTemplate.Transform;
                _template.QuestionTemplate[0] = _questTemplate.Transform;
                _template.SeparatorTemplate[0] = _separatorTemplate.Transform;
                _template.AnswerTemplate[0] = _answerTemplate.Transform;

                _position = 0;
                Progress = 0;
                _stage = Stage.Reading;
                Free = false;

                _task = Task.Factory.StartNew(new Func<Tuple<bool, int>>(Analyze), _tokenSource.Token)
                    .ContinueWith(AnalyzeFinished, _tokenSource.Token, TaskContinuationOptions.ExecuteSynchronously, _scheduler);
                break;

            case Stage.Reading:
                _tokenSource.Cancel();
                OnClosed();
                break;

            case Stage.ReadingResolve:
                Info = Resources.Notice;
                IsEditorOpened = false;
                CanGo = false;
                _skip.CanBeExecuted = false;                
                _stage = Stage.Reading;

                OnHighlightText(0, _text.Length, null, false);

                if (_badTextCopy != _badText)
                {
                    Text = $"{_text[.._position]}{_badText}{_text[(_position + _badLength)..]}";
                    _parts[_readError.Index.Item1][_readError.Index.Item2].Value = _badText;
                    _fileChanged = true;
                }

                AddTemplate(_packageTemplate);
                AddTemplate(_roundTemplate);
                AddTemplate(_themeTemplate);
                AddTemplate(_questTemplate);
                AddTemplate(_separatorTemplate);
                AddTemplate(_answerTemplate);

                foreach (var item in Templates)
                {
                    item.Enabled = false;
                }

                Free = false;

                lock (_sync)
                {
                    Monitor.Pulse(_sync);
                }
                break;

            case Stage.Splitting:
                _tokenSource.Cancel();
                OnClosed();
                break;

            case Stage.SplitResolve:
                GoText = Resources.End;
                _skip.CanBeExecuted = false;
                Info = Resources.Notice;
                Problem = string.Empty;

                IsEditorOpened = false;

                _stage = Stage.Splitting;

                var changedText = _text[_parseError.SourcePosition..];

                if (changedText != _badText)
                {
                    Text = string.Concat(_text.AsSpan(0, _parseError.SourcePosition), _badText);
                    _fileChanged = true;
                }

                Free = false;

                _parseError.Source = Text; // именно так, а не _badText: преобразование откатится на шаг назад, и нам нужен весь текст целиком

                lock (_sync)
                {
                    Monitor.Pulse(_sync);
                }
                break;
        }
    }

    /// <summary>
    /// Добавить новый шаблон в список шаблонов
    /// </summary>
    /// <param name="richTextBox">Редактор с новым шаблоном</param>
    /// <param name="list">Список шаблонов</param>
    private static void AddTemplate(SpardTemplateViewModel template)
    {
        var text = template.Transform;

        if (!template.Enabled || template.Variants.Contains(text))
        {
            return;
        }

        template.Variants.Add(text);
    }

    private void Skip_Executed(object? arg)
    {
        switch (_stage)
        {
            case Stage.SplitResolve:
                Info = Resources.Notice;
                IsEditorOpened = false;
                _stage = Stage.Splitting;

                OnHighlightText(0, _text.Length, null, false);

                _parseError.Skip = true;
                Free = false;

                lock (_sync)
                {
                    Monitor.Pulse(_sync);
                }
                break;

            case Stage.ReadingResolve:
                Info = Resources.Notice;
                IsEditorOpened = false;
                _stage = Stage.Reading;

                OnHighlightText(0, _text.Length, null, false);

                _readError.Skip = true;
                Free = false;

                lock (_sync)
                {
                    Monitor.Pulse(_sync);
                }
                break;
        }
    }

    protected override async Task Close_Executed(object? arg)
    {
        Clean();
        await base.Close_Executed(arg);            
    }

    public void Clean()
    {
        _converter.ParseError -= QTxtConverter_ParseError;
        _converter.ReadError -= QTxtConverter_ReadError;
        _converter.Progress -= QTxtConverter_Progress;

        switch (_stage)
        {
            case Stage.ReadingResolve:
                _readError.Cancel = true;
                break;

            case Stage.SplitResolve:
                _parseError.Cancel = true;
                break;
        }

        lock (_sync)
        {
            Monitor.Pulse(_sync);
        }

        _tokenSource.Cancel();

        if (_task != null && _existing != null)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                string filename = Path.GetFileNameWithoutExtension(FileName);

                UI.Execute(
                    new Action(() =>
                    {
                        var themesNum = _existing.Package.Rounds.Sum(r => r.Themes.Count);
                        var message = $"{Resources.TotalImport} {themesNum}. {Resources.LostFile}{filename}_LostPart.txt?";

                        var save = PlatformManager.Instance.ConfirmExclWithWindow(message);

                        if (save)
                        {
                            using var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(FileName), string.Format("{0}_LostPart.txt", filename)));
                        
                            if (themesNum < _parts.Length)
                            {
                                if (_parts[themesNum].Length > 0)
                                {
                                    writer.Write(_parts[themesNum][^1].Value);
                                }

                                for (int i = themesNum + 1; i < _parts.Length; i++)
                                {
                                    for (int j = 0; j < _parts[i].Length; j++)
                                    {
                                        writer.Write(_parts[i][j].Value);
                                    }
                                }
                            }
                        }
                    }),
                    exc => OnError(exc, ""),
                    CancellationToken.None);

                OnNewItem(new QDocument(_existing, _storageContextViewModel, _loggerFactory) { FileName = _existing.Package.Name });
            }
        }

        // TODO: ask user for a file location to save

        //if (_fileChanged && !string.IsNullOrEmpty(_fileName))
        //{
        //    if (PlatformManager.Instance.Confirm(Resources.SaveFile))
        //    {
        //        using var writer = new StreamWriter(sourePath, false);
        //        writer.Write(_text);
        //        writer.Close();
        //    }
        //}
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e) => OnReadyChanged();

    private void QTxtConverter_ParseError(object? sender, SplitErrorEventArgs e)
    {
        _parseError = e;
        PrepareUI();

        lock (_sync)
        {
            Monitor.Wait(_sync);
        }
    }

    private void QTxtConverter_ReadError(object? sender, ReadErrorEventArgs e)
    {
        _readError = e;
        PrepareUIForRead();

        lock (_sync)
        {
            Monitor.Wait(_sync);
        }
    }

    private void QTxtConverter_Progress(int progress)
    {
        if (_stage == Stage.Reading || _stage == Stage.Splitting)
        {
            _position = progress;

            if (_text.Length > 0)
            {
                Progress = progress * 100 / _text.Length;
            }
        }
        else
        {
            Progress = progress;
        }
    }

    private void OnHighlightText(int start, int length, string? color, bool scroll) => HighlightText?.Invoke(start, length, color, scroll);

    private void PrepareUI()
    {
        _free = true;
        _stage = Stage.SplitResolve;
        CanGo = true;
        GoText = Resources.Futher;
        Info = Resources.SplittingError;
        Problem = Resources.EnumerationError;

        _skip.CanBeExecuted = true;
        SkipToolTip = Resources.NotAQuestionNumber;

        GoodText = _text[0.._parseError.SourcePosition];
        BadText = _text[_parseError.SourcePosition..];
        IsEditorOpened = true;
        OnHighlightText(_parseError.SourcePosition, _text.Length - _parseError.SourcePosition, BadSourceBackColor, true);
    }

    private string GetNormalView(Expression expression)
    {
        if (expression is StringValue stringValue)
        {
            return stringValue.Value;
        }

        if (expression is Set set)
        {
            var name = ((StringValue)((Polynomial)set.Operand).OperandsArray[0]).Value;
            Aliases.TryGetValue(name, out var alias);

            return alias == null ? (name == "Line" ? "\n" : name) : alias.VisibleName;
        }

        if (expression is Optional opt)
        {
            var text = "(";

            if (opt.Operand != null)
            {
                text += GetNormalView(opt.Operand);
            }

            text += ")?";
            return text;
        }

        if (expression is Sequence sequence)
        {
            var text = new StringBuilder();
            Array.ForEach(sequence.OperandsArray, item => text.Append(GetNormalView(item)));
            return text.ToString();
        }

        if (expression is Instruction instruction && instruction.Argument != null)
        {
            return GetNormalView(instruction.Argument);
        }

        if (expression is End close)
        {
            return "Всё распознано, но остался лишний текст";
        }

        return string.Empty;
    }

    private void PrepareUIForRead()
    {
        _free = true;
        SkipToolTip = _readError.Index.Item1 == 0 ? Resources.SkipTitle : Resources.SkipPart;

        IsEditorOpened = true;

        _skip.CanBeExecuted = true;
        CanGo = true;

        var themeIndex = _readError.Index.Item1;
        var questionIndex = _readError.Index.Item2;

        BadText = _parts[themeIndex][questionIndex].Value;

        var fragmentIndex = questionIndex;

        for (int i = 0; i < themeIndex; i++)
        {
            fragmentIndex += _parts[i].Length;
        }
        
        CurrentFragmentIndex = fragmentIndex;

        _badLength = BadText.Length;
        OnHighlightText(0, _readError.BestTry.Index - _readError.Move, BadSourceBackColor, false);

        Info = Resources.PhraseTemplates;

        EditAlias? alias;

        foreach (var item in _readError.BestTry.Match.GetAllMatches())
        {
            if (Aliases.TryGetValue(item.Key, out alias))
            {
                if (item.Value.Index == 0)
                {
                    OnHighlightText(item.Value.Index, item.Value.ToString().Length - _readError.Move, alias.Color, true);
                }
                else
                {
                    OnHighlightText(item.Value.Index - _readError.Move, item.Value.ToString().Length, alias.Color, true);
                }
            }
            else if (int.TryParse(item.Value.ToString(), out int num))
            {
                switch (item.Key)
                {
                    case "p":
                        _packageTemplate.Transform = _template.PackageTemplate[num];
                        break;

                    case "r":
                        _roundTemplate.Transform = _template.RoundTemplate[num];
                        break;

                    case "t":
                        _themeTemplate.Transform = _template.ThemeTemplate[num];
                        break;

                    case "q":
                        _questTemplate.Transform = _template.QuestionTemplate[num];
                        break;

                    case "s":
                        _separatorTemplate.Transform = _template.SeparatorTemplate[num];
                        break;

                    case "a":
                        _answerTemplate.Transform = _template.AnswerTemplate[num];
                        break;
                }
            }
        }

        _packageTemplate.Enabled = _readError.Index.Item1 == 0;
        _roundTemplate.Enabled = _themeTemplate.Enabled = _readError.Index.Item2 == _parts[_readError.Index.Item1].Length - 1;
        _questTemplate.Enabled = true;

        _packageTemplate.CanChange = _packageTemplate.Enabled && _template.PackageTemplate.Count > 1;
        _roundTemplate.CanChange = _roundTemplate.Enabled && _template.RoundTemplate.Count > 1;
        _themeTemplate.CanChange = _themeTemplate.Enabled && _template.ThemeTemplate.Count > 1;
        _questTemplate.CanChange = _questTemplate.Enabled && _template.QuestionTemplate.Count > 1;
        _separatorTemplate.CanChange = _separatorTemplate.Enabled && _template.SeparatorTemplate.Count > 1;
        _answerTemplate.CanChange = _answerTemplate.Enabled && _template.AnswerTemplate.Count > 1;

        _stage = Stage.ReadingResolve;
        _badTextCopy = _badText;

        var problem = string.Format(" [{0}: {1}]", Resources.UnreadTemplate, GetNormalView(_readError.NotReaded));

        // Определение причины ошибки
        if (_readError.Missing is StringValue str)
        {
            Problem = string.Format(Resources.FragmentNotFound, str.Value.Replace(" ", Resources.Space))
                 + Environment.NewLine + problem + Environment.NewLine + Resources.SourceFail;

            return;
        }

        if (_readError.Missing is Set set)
        {
            var setName = set.Operand.Operands().First().ToString();

            if (Aliases.TryGetValue(setName, out alias))
            {
                Problem = string.Format(Resources.ObjectNotFound, alias.VisibleName) + Environment.NewLine + problem;
            }
            else
            {
                switch (setName)
                {
                    case "Line":
                        setName = Resources.NewLine;
                        break;

                    case "SP":
                        setName = Resources.Space;
                        break;
                }

                Problem = string.Format(Resources.ObjectNotFound, setName) + Environment.NewLine + problem + Environment.NewLine + Resources.SourceFail;
            }

            return;
        }

        Problem = string.Format(
            Resources.FragmentNotFound,
            _readError.Missing.ToString().Replace(" ", Resources.Space))
            + Environment.NewLine + problem + Environment.NewLine + Resources.SourceFail;
    }

    private void Split()
    {
        try
        {
            _parts = _converter.ExtractQuestions(_text);
            Progress = 0;
            
            if (_parts == null || _parts.Length == 1)
            {
                if (!_tokenSource.IsCancellationRequested)
                {
                    PlatformManager.Instance.ShowExclamationMessage(Resources.NoQuestionsFound);
                }

                return;
            }

            Free = true;

            GoText = Resources.Start;
            _skip.CanBeExecuted = false;
            CanGo = true;
            CanChangeStandart = true;
            _stage = Stage.Begin;
            Problem = "";
            Info = Resources.Notice;
            State = UIState.Parse;
            Fragments = _parts.SelectMany(p => p).Select(p => p.Value).ToArray();
            BadText = "";

            OnHighlightText(0, _text.Length, null, true);

            if (_automaticTextImport)
            {
                _auto.Execute(null);
            }
        }
        catch (Exception exc)
        {
            MainViewModel.ShowError(exc);
        }
    }

    private void SetTemplate(SITemplate template)
    {
        _template = template;

        _packageTemplate.Transform = _template.PackageTemplate[0];
        _roundTemplate.Transform = _template.RoundTemplate[0];
        _themeTemplate.Transform = _template.ThemeTemplate[0];
        _questTemplate.Transform = _template.QuestionTemplate[0];
        _separatorTemplate.Transform = _template.SeparatorTemplate[0];
        _answerTemplate.Transform = _template.AnswerTemplate[0];

        _packageTemplate.Variants = new ObservableCollection<string>(_template.PackageTemplate);
        _roundTemplate.Variants = new ObservableCollection<string>(_template.RoundTemplate);
        _themeTemplate.Variants = new ObservableCollection<string>(_template.ThemeTemplate);
        _questTemplate.Variants = new ObservableCollection<string>(_template.QuestionTemplate);
        _separatorTemplate.Variants = new ObservableCollection<string>(_template.SeparatorTemplate);
        _answerTemplate.Variants = new ObservableCollection<string>(_template.AnswerTemplate);

        BindHelper.Bind(_packageTemplate.Variants, _template.PackageTemplate);
        BindHelper.Bind(_roundTemplate.Variants, _template.RoundTemplate);
        BindHelper.Bind(_themeTemplate.Variants, _template.ThemeTemplate);
        BindHelper.Bind(_questTemplate.Variants, _template.QuestionTemplate);
        BindHelper.Bind(_separatorTemplate.Variants, _template.SeparatorTemplate);
        BindHelper.Bind(_answerTemplate.Variants, _template.AnswerTemplate);
    }
}
