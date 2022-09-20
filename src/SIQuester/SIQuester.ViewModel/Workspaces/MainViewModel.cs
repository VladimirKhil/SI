using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIStorageService.Client;
using SIStorageService.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Utils;

namespace SIQuester.ViewModel
{
    public sealed class MainViewModel : ModelViewBase, INotifyPropertyChanged
    {
        private const string DonateUrl = "https://yoomoney.ru/embed/shop.xml?account=410012283941753&quickpay=shop&payment-type-choice=on&writer=seller&targets=%D0%9F%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%BA%D0%B0+%D0%B0%D0%B2%D1%82%D0%BE%D1%80%D0%B0&targets-hint=&default-sum=100&button-text=03&comment=on&hint=%D0%92%D0%B0%D1%88+%D0%BA%D0%BE%D0%BC%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%80%D0%B8%D0%B9";
        private const int MaxMessageLength = 1000;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MainViewModel> _logger;

        #region Commands

        public ICommand Open { get; private set; }

        /// <summary>
        /// Open one of the recently opened files.
        /// </summary>
        public ICommand OpenRecent { get; private set; }

        /// <summary>
        /// Remove one of the recentlty opened files.
        /// </summary>
        public ICommand RemoveRecent { get; private set; }

        /// <summary>
        /// Импортировать текстовый файл
        /// </summary>
        public ICommand ImportTxt { get; private set; }
        /// <summary>
        /// Импортировать текст из буфера обмена
        /// </summary>
        public ICommand ImportClipboardTxt { get; private set; }
        /// <summary>
        /// Импортировать XML-файл
        /// </summary>
        public ICommand ImportXml { get; private set; }
        /// <summary>
        /// Импорт из Базы вопросов
        /// </summary>
        public ICommand ImportBase { get; private set; }
        /// <summary>
        /// Импорт пакета из хранилища СИ
        /// </summary>
        public ICommand ImportFromSIStore { get; private set; }

        /// <summary>
        /// Сохранить всё
        /// </summary>
        public SimpleCommand SaveAll { get; private set; }

        public ICommand About { get; private set; }
        public ICommand Feedback { get; private set; }
        public ICommand Donate { get; private set; }
        public ICommand SetSettings { get; private set; }
        public ICommand SearchFolder { get; private set; }

        #endregion

        /// <summary>
        /// Открытые документы
        /// </summary>
        public ObservableCollection<WorkspaceViewModel> DocList { get; private set; }

        private QDocument _activeDocument = null;

        /// <summary>
        /// Текущий редактируемый документ
        /// </summary>
        public QDocument ActiveDocument
        {
            get => _activeDocument;
            set
            {
                if (_activeDocument != value)
                {
                    _activeDocument = value;
                    OnPropertyChanged();
                }
            }
        }

        public AppSettings Settings => AppSettings.Default;

        private readonly string[] _args = null;

        private readonly StorageContextViewModel _storageContextViewModel;
        
        public MainViewModel(string[] args, ISIStorageServiceClient siStorageServiceClient, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MainViewModel>();

            DocList = new ObservableCollection<WorkspaceViewModel>();
            DocList.CollectionChanged += DocList_CollectionChanged;
            CollectionViewSource.GetDefaultView(DocList).CurrentChanged += MainViewModel_CurrentChanged;

            Open = new SimpleCommand(Open_Executed);
            OpenRecent = new SimpleCommand(OpenRecent_Executed);
            RemoveRecent = new SimpleCommand(RemoveRecent_Executed);

            ImportTxt = new SimpleCommand(ImportTxt_Executed);
            ImportClipboardTxt = new SimpleCommand(ImportClipboardTxt_Executed);
            ImportXml = new SimpleCommand(ImportXml_Executed);
            ImportBase = new SimpleCommand(ImportBase_Executed);
            ImportFromSIStore = new SimpleCommand(ImportFromSIStore_Executed);

            SaveAll = new SimpleCommand(SaveAll_Executed) { CanBeExecuted = false };

            About = new SimpleCommand(About_Executed);
            Feedback = new SimpleCommand(Feedback_Executed);
            Donate = new SimpleCommand(Donate_Executed);

            SetSettings = new SimpleCommand(SetSettings_Executed);
            SearchFolder = new SimpleCommand(SearchFolder_Executed);

            _storageContextViewModel = new StorageContextViewModel(siStorageServiceClient);
            _storageContextViewModel.Load();

            AddCommandBinding(ApplicationCommands.New, New_Executed);
            AddCommandBinding(ApplicationCommands.Open, (sender, e) => Open_Executed(e.Parameter));
            AddCommandBinding(ApplicationCommands.Help, Help_Executed);
            AddCommandBinding(ApplicationCommands.Close, Close_Executed);

            AddCommandBinding(ApplicationCommands.SaveAs, (s, e) => ActiveDocument?.SaveAs_Executed(), CanExecuteDocumentCommand);

            AddCommandBinding(ApplicationCommands.Copy, (s, e) => ActiveDocument?.Copy_Executed(), CanExecuteDocumentCommand);
            AddCommandBinding(ApplicationCommands.Paste, (s, e) => ActiveDocument?.Paste_Executed(), CanExecuteDocumentCommand);

            _args = args;

            UI.Initialize();
        }

        private void CanExecuteDocumentCommand(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = ActiveDocument != null;

        public async void AutoSave(int autoSaveCounter)
        {
            foreach (var item in DocList.ToArray())
            {
                try
                {
                    await item.SaveIfNeededAsync(true, autoSaveCounter % 3 == 0); // Every n-th autosave is full
                }
                catch (Exception exc)
                {
                    ShowError(exc);
                }
            }
        }

        public void Initialize()
        {
            if (_args.Length > 0)
            {
                OpenFile(_args[0]);
            }

            if (AppSettings.Default.AutoSave)
            {
                var autoSaveFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.AutoSaveFolderName));
                
                if (!autoSaveFolder.Exists)
                {
                    return;
                }

                var files = autoSaveFolder.EnumerateFiles();

                if (files.Any())
                {
                    try
                    {
                        _logger.LogInformation("Unsaved files found");

                        if (PlatformManager.Instance.Confirm(Resources.RestoreConfirmation))
                        {
                            var restoreFolder = Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.AutoSaveRestoreFolderName);
                            
                            Directory.CreateDirectory(restoreFolder);

                            foreach (var file in files)
                            {
                                var tmpName = Path.Combine(restoreFolder, Guid.NewGuid().ToString());

                                File.Copy(file.FullName, tmpName, true);

                                _logger.LogInformation("Restoring file {file}...", file.FullName);

                                OpenFile(tmpName, overridePath: PathHelper.DecodePath(file.Name));
                            }
                        }
                        else
                        {
                            foreach (var file in files)
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        ShowError(exc);
                    }
                }
            }
        }

        private void SearchFolder_Executed(object arg) => DocList.Add(new SearchFolderViewModel(this));

        private void SetSettings_Executed(object arg) => DocList.Add(new SettingsViewModel());

        private void Help_Executed(object sender, ExecutedRoutedEventArgs e) => DocList.Add(new HowToViewModel());

        private async void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (await DisposeRequestAsync())
            {
                PlatformManager.Instance.Exit();
            }
        }

        public async Task<bool> DisposeRequestAsync()
        {
            foreach (var doc in DocList.ToArray())
            {
                await doc.Close.ExecuteAsync(null);

                if (DocList.Contains(doc)) // Закрытие отменено
                {
                    return false;
                }
            }

            return true;
        }

        private void Feedback_Executed(object arg) => OpenUri(Resources.AuthorSiteUrl);

        private void Donate_Executed(object arg) => OpenUri(DonateUrl);

        private static void OpenUri(string uri)
        {
            try
            {
                Browser.Open(uri);
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
        }

        private void About_Executed(object arg) => DocList.Add(new AboutViewModel());

        private void MainViewModel_CurrentChanged(object sender, EventArgs e)
        {
            ActiveDocument = CollectionViewSource.GetDefaultView(DocList).CurrentItem as QDocument;
        }

        private void DocList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (WorkspaceViewModel item in e.NewItems)
                    {
                        item.Error += ShowError;
                        item.NewItem += Item_NewDoc;
                        item.Closed += Item_Closed;
                    }

                    CollectionViewSource.GetDefaultView(DocList).MoveCurrentToLast();
                    CheckSaveAllCanBeExecuted(this, EventArgs.Empty);
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (WorkspaceViewModel item in e.OldItems)
                    {
                        item.Error -= ShowError;
                        item.NewItem -= Item_NewDoc;
                        item.Closed -= Item_Closed;
                    }

                    CheckSaveAllCanBeExecuted(this, EventArgs.Empty);
                    break;

                default:
                    break;
            }
        }

        private void Item_NewDoc(WorkspaceViewModel doc)
        {
            DocList.Add(doc);
        }

        private void Item_Closed(WorkspaceViewModel doc)
        {
            doc.Dispose();
            DocList.Remove(doc);
        }

        private void CheckSaveAllCanBeExecuted(object sender, EventArgs e)
        {
            SaveAll.CanBeExecuted = DocList.Count > 0;
        }

        /// <summary>
        /// Новый
        /// </summary>
        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DocList.Add(new NewViewModel(_storageContextViewModel, _loggerFactory));
        }

        /// <summary>
        /// Открыть существующий пакет
        /// </summary>
        private void Open_Executed(object arg)
        {
            if (arg is string filename)
            {
                OpenFile(filename);
                return;
            }

            var files = PlatformManager.Instance.ShowOpenUI();

            if (files != null)
            {
                foreach (var file in files)
                {
                    OpenFile(file);
                }
            }
        }

        /// <summary>
        /// Открыть недавний файл
        /// </summary>
        /// <param name="arg">Путь к файлу</param>
        private void OpenRecent_Executed(object arg) => OpenFile(arg.ToString());

        /// <summary>
        /// Removes recent file.
        /// </summary>
        /// <param name="arg">Recent file path.</param>
        private void RemoveRecent_Executed(object arg) => AppSettings.Default.History.Remove(arg.ToString());

        /// <summary>
        /// Opens the existing file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="search">Text to search in file after opening.</param>
        /// <param name="overridePath">Overriden file path which will be used for file saving.</param>
        /// <param name="onSuccess">Action to execute after successfull opening.</param>
        internal void OpenFile(string path, string search = null, string overridePath = null, Action onSuccess = null)
        {
            var savingPath = overridePath ?? path;

            Task<QDocument> loader(CancellationToken cancellationToken) => Task.Run(() =>
            {
                FileStream stream = null;
                try
                {
                    stream = File.OpenRead(path);

                    // Loads in read only mode to keep file LastUpdate time unmodified
                    var doc = SIDocument.Load(stream);

                    _logger.LogInformation("Document has been successfully opened. Path: {path}", path);

                    var docViewModel = new QDocument(doc, _storageContextViewModel, _loggerFactory)
                    {
                        Path = savingPath,
                        FileName = Path.GetFileNameWithoutExtension(savingPath)
                    };

                    if (search != null)
                    {
                        docViewModel.SearchText = search;
                    }

                    if (overridePath != null)
                    {
                        docViewModel.OverridePath = overridePath;
                        docViewModel.OriginalPath = path;
                        docViewModel.Changed = true;
                    }

                    docViewModel.CheckFileSize();

                    return docViewModel;
                }
                catch (Exception exc)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }

                    if (exc is UnauthorizedAccessException && (new FileInfo(path).Attributes & FileAttributes.ReadOnly) > 0)
                    {
                        throw new Exception(Resources.FileIsReadOnly, exc);
                    }

                    throw;
                }
            }).ContinueWith(
                task =>
                {
                    if (task.IsFaulted)
                    {
                        var exc = task.Exception.InnerException;

                        if (exc is FileNotFoundException)
                        {
                            AppSettings.Default.History.Remove(path);
                        }

                        _logger.LogError(exc, "File open error: {error}", exc.Message);

                        throw exc;
                    }

                    return task.Result;
                },
                TaskScheduler.FromCurrentSynchronizationContext());

            DocList.Add(
                new DocumentLoaderViewModel(
                    path,
                    loader,
                    () =>
                    {
                        AppSettings.Default.History.Add(savingPath);

                        onSuccess?.Invoke();
                    }));
        }

        private void ImportTxt_Executed(object arg)
        {
            var model = new ImportTextViewModel(arg, _storageContextViewModel, _loggerFactory);
            DocList.Add(model);
            model.Start();
        }

        /// <summary>
        /// Импортировать текст из буфера обмена
        /// </summary>
        private void ImportClipboardTxt_Executed(object arg)
        {
            var model = new ImportTextViewModel(typeof(Clipboard), _storageContextViewModel, _loggerFactory);
            DocList.Add(model);
            model.Start();
        }

        private void ImportXml_Executed(object arg)
        {
            var file = PlatformManager.Instance.ShowImportXmlUI();

            if (file == null)
            {
                return;
            }

            try
            {
                using var stream = File.OpenRead(file);
                var doc = SIDocument.LoadXml(stream);

                var docViewModel = new QDocument(doc, _storageContextViewModel, _loggerFactory)
                {
                    Path = "",
                    Changed = true,
                    FileName = Path.GetFileNameWithoutExtension(file)
                };

                LoadMediaFromFolder(docViewModel, Path.GetDirectoryName(file));

                DocList.Add(docViewModel);
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
        }

        private static void LoadMediaFromFolder(QDocument document, string folder)
        {
            // Загрузим файлы контента при наличии ссылок на них
            foreach (var round in document.Package.Rounds)
            {
                foreach (var theme in round.Themes)
                {
                    foreach (var question in theme.Questions)
                    {
                        foreach (var atom in question.Scenario)
                        {
                            if (atom.Model.IsLink)
                            {
                                var link = document.Document.GetLink(atom.Model);

                                var collection = document.Images;

                                switch (atom.Model.Type)
                                {
                                    case AtomTypes.Audio:
                                        collection = document.Audio;
                                        break;

                                    case AtomTypes.Video:
                                        collection = document.Video;
                                        break;
                                }

                                if (collection.Files.Any(f => f.Model.Name == link.Uri))
                                {
                                    continue;
                                }

                                var resFileName = Path.Combine(folder, link.Uri);

                                if (!File.Exists(resFileName))
                                {
                                    continue;
                                }

                                collection.AddFile(resFileName);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Imports package from Packages Database.
        /// </summary>
        private void ImportBase_Executed(object arg) => DocList.Add(new ImportDBStorageViewModel(_storageContextViewModel, _loggerFactory));

        /// <summary>
        /// Imports package from SI Storage.
        /// </summary>
        private async void ImportFromSIStore_Executed(object arg)
        {
            var importViewModel = new ImportSIStorageViewModel(
                _storageContextViewModel,
                PlatformManager.Instance.ServiceProvider.GetRequiredService<SIStorage>(),
                _loggerFactory);

            DocList.Add(importViewModel);

            await importViewModel.OpenAsync();
        }

        private async void SaveAll_Executed(object arg)
        {
            foreach (var item in DocList.ToArray())
            {
                try
                {
                    await item.SaveIfNeededAsync(false, true);
                }
                catch (Exception exc)
                {
                    ShowError(exc);
                }
            }
        }        

        public static void ShowError(Exception exc, string message = null)
        {
            var fullMessage = message != null ? $"{message}: {exc.Message}" : exc.Message;

            if (fullMessage.Length > MaxMessageLength)
            {
                fullMessage = string.Concat(fullMessage.AsSpan(0, MaxMessageLength), "…");
            }

            PlatformManager.Instance.ShowExclamationMessage(fullMessage);
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var item in DocList)
            {
                item.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
