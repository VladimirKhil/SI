using Lingware.Spard.Expressions;
using QTxtConverter;
using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Core;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Импорт текстового файла
    /// </summary>
    public sealed class ImportTextViewModel : WorkspaceViewModel
    {
        private readonly object _arg = null;
        private Task _task = null;

        public override string Header => "Импорт текста";

        private string _text = "";

        public string Text
        {
            get { return _text; }
            set { _text = value; OnPropertyChanged(); }
        }

        private string _badText = "";

        public string BadText
        {
            get { return _badText; }
            set { _badText = value; OnPropertyChanged(); }
        }
        
        private bool _free = false;

        public bool Free
        {
            get
            {
                return _free;
            }
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
            get { return _standartLogic; }
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
        
        private string _goText;

        public string GoText
        {
            get { return _goText; }
            set { _goText = value; OnPropertyChanged(); }
        }
        
        private bool _isEditorOpened;

        public bool IsEditorOpened
        {
            get { return _isEditorOpened; }
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
            get { return _progress; }
            set { if (_progress != value) { _progress = value; OnPropertyChanged(); } }
        }
                
        /// <summary>
        /// Шаблоны распознавания
        /// </summary>
        public ObservableCollection<SpardTemplateViewModel> Templates { get; set; }

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

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly TaskScheduler _scheduler = null;

        private readonly StorageContextViewModel _storageContextViewModel;

        private string _badTextCopy = "";

        private readonly QConverter _converter = new QConverter();

        private SIDocument _existing = null;
        private string _path = string.Empty;
        private bool _automaticTextImport = false;

        private SIPart[][] _parts = null;
        private SITemplate _template = null;

        private readonly object _sync = new object();

        private static Color BadSourceBackColor = Colors.Wheat;

        /// <summary>
        /// Стадия работы распознавателя
        /// </summary>
        private enum Stage
        {
            /// <summary>
            /// Разделение текста на темы и вопросы
            /// </summary>        
            Splitting,
            SplitResolve,
            Automation,
            Begin,
            Reading,
            ReadingResolve,
            None
        };

        private Stage _stage = Stage.Splitting;

        private ParseErrorEventArgs _parseError = null;
        private ReadErrorEventArgs _readError = null;

        private int _badLength = 0;

        private bool _fileChanged = false;

        private Dictionary<string, EditAlias> Aliases { get; } = new Dictionary<string, EditAlias>();

        private string _info = "";

        public string Info
        {
            get { return _info; }
            set { _info = value; OnPropertyChanged(); }
        }

        private string _problem;

        public string Problem
        {
            get { return _problem; }
            set { _problem = value; OnPropertyChanged(); }
        }

        private bool _canChangeStandart = false;

        public bool CanChangeStandart
        {
            get { return _canChangeStandart; }
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

        private string _skipToolTip;

        public string SkipToolTip
        {
            get { return _skipToolTip; }
            set { _skipToolTip = value; OnPropertyChanged(); }
        }

        public event Action<int, int, Color?, bool> SelectText;

        public ImportTextViewModel(object arg, StorageContextViewModel storageContextViewModel)
        {
            _arg = arg;
            _storageContextViewModel = storageContextViewModel;
            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            #region Aliases
            Templates = new ObservableCollection<SpardTemplateViewModel>();

            var trashAlias = new EditAlias("Мусор", Colors.LightGray);

            _packageTemplate = new SpardTemplateViewModel { Name = "Пакет" };
            _packageTemplate.Aliases["PName"] = new EditAlias("Пакет", Colors.Orchid);
            _packageTemplate.Aliases["Some"] = trashAlias;
            Templates.Add(_packageTemplate);

            _roundTemplate = new SpardTemplateViewModel { Name = "Раунд" };
            _roundTemplate.Aliases["RName"] = new EditAlias("Раунд", Colors.Olive);
            _roundTemplate.Aliases["Some"] = trashAlias;
            Templates.Add(_roundTemplate);

            _themeTemplate = new SpardTemplateViewModel { Name = "Тема" };
            _themeTemplate.Aliases["TName"] = new EditAlias("Тема", Colors.BlueViolet);
            _themeTemplate.Aliases["TAuthor"] = new EditAlias("Автор", Colors.Maroon);
            _themeTemplate.Aliases["TComment"] = new EditAlias("Комментарий", Colors.Navy);
            _themeTemplate.Aliases["Some"] = trashAlias;
            Templates.Add(_themeTemplate);

            _questTemplate = new SpardTemplateViewModel { Name = "Вопрос" };
            _questTemplate.Aliases["Number"] = new EditAlias("Номер", Colors.SkyBlue);
            _questTemplate.Aliases["QText"] = new EditAlias("Вопрос", Colors.SeaGreen);
            _questTemplate.Aliases["Answer"] = new EditAlias("Ответ", Colors.Yellow);
            _questTemplate.Aliases["QAuthor"] = new EditAlias("Автор", Colors.Goldenrod);
            _questTemplate.Aliases["QComment"] = new EditAlias("Комментарий", Colors.Cyan);
            _questTemplate.Aliases["QSource"] = new EditAlias("Источник", Colors.Chocolate);
            _questTemplate.Aliases["Some"] = trashAlias;
            Templates.Add(_questTemplate);

            _separatorTemplate = new SpardTemplateViewModel { Name = "Разделитель", NonStandartOnly = true };
            _separatorTemplate.Aliases["Some"] = trashAlias;            

            _answerTemplate = new SpardTemplateViewModel { Name = "Ответ", NonStandartOnly = true };

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
                        Aliases[alias.Key] = alias.Value;
                }
            }

            #endregion

            _sns = new SimpleCommand(Sns_Executed) { CanBeExecuted = false };
            _auto = new SimpleCommand(Auto_Executed) { CanBeExecuted = false };
            _go = new SimpleCommand(Go_Executed);
            _skip = new SimpleCommand(Skip_Executed);
        }

        private void Sns_Executed(object arg)
        {
            SetTemplate(QConverter.GetSnsTemplates(_parts, _standartLogic));
        }

        private async void Auto_Executed(object arg)
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
                        OnNewItem(new QDocument(_existing, _storageContextViewModel) { FileName = _existing.Package.Name });
                    }
                }
            }

            _task = null;
            OnClosed();
        }

        private Tuple<bool, int> Analyze()
        {
            var result = _converter.ReadFile(_parts, _template, ref _existing, false, string.IsNullOrEmpty(_path) ? "Безымянный" : Path.GetFileNameWithoutExtension(_path), Resources.Empty, Resources.ThemesCollection, out int themesNum);

            return Tuple.Create(result, themesNum);
        }

        private void Go_Executed(object arg)
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

                    _task = Task.Factory.StartNew(new Func<Tuple<bool, int>>(Analyze), _tokenSource.Token).ContinueWith(AnalyzeFinished, _tokenSource.Token, TaskContinuationOptions.ExecuteSynchronously, _scheduler);
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

                    OnSelectText(0, _text.Length, null, false);
                    if (_badTextCopy != _badText)
                    {
                        Text = $"{_text.Substring(0, _position)}{_badText}{_text.Substring(_position + _badLength)}";
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

                    var changedText = _text.Substring(_parseError.SourcePosition);
                    if (changedText != _badText)
                    {
                        Text = _text.Substring(0, _parseError.SourcePosition) + _badText;
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
                return;

            template.Variants.Add(text);
        }

        private void Skip_Executed(object arg)
        {
            switch (_stage)
            {
                case Stage.SplitResolve:
                    Info = Resources.Notice;

                    IsEditorOpened = false;

                    _stage = Stage.Splitting;

                    OnSelectText(0, _text.Length, null, false);

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
                    OnSelectText(0, _text.Length, null, false);

                    _readError.Skip = true;

                    Free = false;

                    lock (_sync)
                    {
                        Monitor.Pulse(_sync);
                    }
                    break;
            }
        }

        protected override async Task Close_Executed(object arg)
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
                if (!string.IsNullOrEmpty(_path))
                {
                    string filename = Path.GetFileNameWithoutExtension(_path);
                    Task.Factory.StartNew(new Action(() =>
                    {
                        var themesNum = _existing.Package.Rounds.Sum(r => r.Themes.Count);
                        var message = $"{Resources.TotalImport} {themesNum}. {Resources.LostFile}{filename}_LostPart.txt?";

                        var save = PlatformManager.Instance.ConfirmExclWithWindow(message);

                        if (save)
                        {                            
                            using (var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(_path), string.Format("{0}_LostPart.txt", filename))))
                            {
                                if (themesNum < _parts.Length)
                                {
                                    if (_parts[themesNum].Length > 0)
                                        writer.Write(_parts[themesNum][_parts[themesNum].Length - 1].Value);

                                    for (int i = themesNum + 1; i < _parts.Length; i++)
                                    {
                                        for (int j = 0; j < _parts[i].Length; j++)
                                        {
                                            writer.Write(_parts[i][j].Value);
                                        }
                                    }
                                }
                            }
                        }
                    }), CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);

                    OnNewItem(new QDocument(_existing, _storageContextViewModel) { FileName = _existing.Package.Name });
                }
            }

            if (_fileChanged && !string.IsNullOrEmpty(_path))
            {
                if (PlatformManager.Instance.Confirm(Resources.SaveFile))
                {
                    using (var writer = new StreamWriter(_path, false, System.Text.Encoding.GetEncoding(1251)))
                    {
                        writer.Write(_text);
                        writer.Close();
                    }
                }
            }
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnReadyChanged();
        }

        internal void Start()
        {
            _automaticTextImport = AppSettings.Default.AutomaticTextImport;

            if (_arg == null)
                ImportNew();
            else if (_arg is Stream)
                ImportFromFile(null, _arg as Stream);
            else if (_arg as Type == typeof(System.Windows.Clipboard))
            {
                if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText))
                    Text = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
                else if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text))
                    Text = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            }
            else
                ImportFromFile(_arg.ToString(), null);

            _converter.ParseError += QTxtConverter_ParseError;
            _converter.ReadError += QTxtConverter_ReadError;
            _converter.Progress += QTxtConverter_Progress;

            Free = false;
            CanChangeStandart = false;
            _skip.CanBeExecuted = false;

            Task.Factory.StartNew(Split, _tokenSource.Token);
        }

        public void ImportNew()
        {
            var file = PlatformManager.Instance.ShowImportUI();
            if (file == null)
            {
                OnClosed();
                return;
            }

            ImportFromFile(file, null);
        }

        public void ImportFromFile(string filename, Stream source)
        {
            _path = filename;

            try
            {
                if (!string.IsNullOrEmpty(_path))
                {
                    Text = File.ReadAllText(_path, Encoding.GetEncoding(1251));
                }
                else if (source != null)
                {
                    using (var reader = new StreamReader(source, Encoding.GetEncoding(1251)))
                    {
                        Text = reader.ReadToEnd();
                    }
                }
                else
                    Text = string.Empty;
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowErrorMessage(exc.Message);
                OnClosed();
            }
        }

        private void QTxtConverter_ParseError(object sender, ParseErrorEventArgs e)
        {
            _parseError = e;
            PrepareUI();

            lock (_sync)
            {
                Monitor.Wait(_sync);
            }
        }

        private void QTxtConverter_ReadError(object sender, ReadErrorEventArgs e)
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
                    Progress = progress * 100 / _text.Length;
            }
            else
                Progress = progress;
        }

        private void OnSelectText(int start, int length, Color? color, bool scroll)
        {
            SelectText?.Invoke(start, length, color, scroll);
        }

        private void PrepareUI()
        {
            _free = true;
            _stage = Stage.SplitResolve;
            CanGo = true;
            GoText = Resources.Futher;
            Info = Resources.PhraseTemplates;
            Problem = Resources.EnumerationError;

            _skip.CanBeExecuted = true;
            SkipToolTip = Resources.NotAQuestionNumber;

            BadText = _text.Substring(_parseError.SourcePosition);
            IsEditorOpened = true;
            OnSelectText(_parseError.SourcePosition, _text.Length - _parseError.SourcePosition, BadSourceBackColor, true);
        }

        private string GetNormalView(Expression expression)
        {
            if (expression is StringValue stringValue)
                return stringValue.Value;

            if (expression is Set set)
            {
                var name = ((StringValue)((Polynomial)set.Operand).OperandsArray[0]).Value;
                Aliases.TryGetValue(name, out EditAlias alias);

                return alias == null ? (name == "Line" ? "\n" : name) : alias.VisibleName;
            }

            if (expression is Optional opt)
            {
                var text = "(";

                if (opt.Operand != null)
                    text += GetNormalView(opt.Operand);

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
            BadText = _parts[_readError.Index.Item1][_readError.Index.Item2].Value;

            int position = _position;
            if (!_template.StandartLogic && _readError.Index.Item1 != 0 && _readError.Index.Item1 % 2 == 0)
            {
                for (int j = _readError.Index.Item2; j < _parts[_readError.Index.Item1 - 1].Length; j++)
                {
                    position += _parts[_readError.Index.Item1 - 1][j].Value.Length;
                }

                for (int j = 0; j < _readError.Index.Item2; j++)
                {
                    position += _parts[_readError.Index.Item1][j].Value.Length;
                }
            }

            _badLength = BadText.Length;
            OnSelectText(position, _readError.BestTry.Index - _readError.Move, BadSourceBackColor, false);

            Info = Resources.PhraseTemplates;

            EditAlias alias;
            foreach (var item in _readError.BestTry.Match.GetAllMatches())
            {
                if (Aliases.TryGetValue(item.Key, out alias))
                {
                    if (item.Value.Index == 0)
                        OnSelectText(position + item.Value.Index, item.Value.ToString().Length - _readError.Move, alias.Color, true);
                    else
                        OnSelectText(position + item.Value.Index - _readError.Move, item.Value.ToString().Length, alias.Color, true);
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
                    Problem = string.Format(Resources.ObjectNotFound, alias.VisibleName) + Environment.NewLine + problem;
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

                    Problem = string.Format(Resources.ObjectNotFound, setName)
                        + Environment.NewLine + problem + Environment.NewLine + Resources.SourceFail;
                }
                return;
            }

            Problem = string.Format(Resources.FragmentNotFound, _readError.Missing.ToString().Replace(" ", Resources.Space))
                + Environment.NewLine + problem + Environment.NewLine + Resources.SourceFail;
        }

        private void Split()
        {
            try
            {
                _parts = _converter.ExtractQuestions(_text);

                Progress = 0;
                
                if (_parts != null && _parts.Length == 1)
                {
                    if (!_tokenSource.IsCancellationRequested)
                        PlatformManager.Instance.ShowExclamationMessage(Resources.NoQuestionsFound);

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

                OnSelectText(0, _text.Length, null, true);

                if (_automaticTextImport)
                    _auto.Execute(null);
            }
            catch (Exception exc)
            {
                MainViewModel.ShowError(exc);
            }
        }

        #region ImportFormNew

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

        #endregion
    }
}
