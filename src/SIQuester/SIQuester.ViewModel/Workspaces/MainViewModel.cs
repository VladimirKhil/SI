using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows;
using System.Xml;
using Notions;
using System.Diagnostics;
using System.IO;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using SIPackages;
using System.Collections.ObjectModel;
using SIQuester.ViewModel.Core;
using SIQuester.ViewModel.PlatformSpecific;
using System.Threading.Tasks;
using SIPackages.Core;

namespace SIQuester.ViewModel
{
    public sealed class MainViewModel: ModelViewBase, INotifyPropertyChanged
    {
        #region Commands

        public ICommand Open { get; private set; }
        public ICommand OpenRecent { get; private set; }

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
            get { return _activeDocument; }
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
        
        public MainViewModel(string[] args)
        {
            DocList = new ObservableCollection<WorkspaceViewModel>();
            DocList.CollectionChanged += DocList_CollectionChanged;
            CollectionViewSource.GetDefaultView(DocList).CurrentChanged += MainViewModel_CurrentChanged;

            Open = new SimpleCommand(Open_Executed);
            OpenRecent = new SimpleCommand(OpenRecent_Executed);

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

            _storageContextViewModel = new StorageContextViewModel(new Services.SI.SIStorageService());
            _storageContextViewModel.Load();

            AddCommandBinding(ApplicationCommands.New, New_Executed);
            AddCommandBinding(ApplicationCommands.Open, (sender, e) => Open_Executed(e.Parameter));
            AddCommandBinding(ApplicationCommands.Help, Help_Executed);
            AddCommandBinding(ApplicationCommands.Close, Close_Executed);

            _args = args;

            UI.Initialize();
        }

        public async void AutoSave()
        {
            foreach (var item in DocList.ToArray())
            {
                try
                {
                    await item.SaveIfNeeded(true);
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
                var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "SIQuester"));
                if (!tempDir.Exists)
                    return;

                if (tempDir.EnumerateFiles().Any())
                {
                    try
                    {
                        if (PlatformManager.Instance.Confirm("Обнаружены файлы, которые не были корректно сохранены при последней работе с программой. Восстановить их?"))
                        {
                            foreach (var file in tempDir.EnumerateFiles())
                            {
                                var tmpName = Path.Combine(Path.GetTempPath(), "SIQuester", Guid.NewGuid().ToString());
                                File.Copy(file.FullName, tmpName);
                                OpenFile(tmpName, overridePath: QDocument.DecodePath(file.Name));
                            }
                        }
                        else
                        {
                            var files = tempDir.EnumerateFiles().ToArray();
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

        private void SearchFolder_Executed(object arg)
        {
            DocList.Add(new SearchFolderViewModel(this));
        }

        private void SetSettings_Executed(object arg)
        {
            DocList.Add(new SettingsViewModel());
        }

        private void Help_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DocList.Add(new HowToViewModel());
        }

        private async void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (await DisposeRequest())
                PlatformManager.Instance.Exit();
        }

        public async Task<bool> DisposeRequest()
        {
            foreach (var doc in DocList.ToArray())
            {
                await doc.Close.ExecuteAsync(null);
                if (DocList.Contains(doc)) // Закрытие отменено
                    return false;
            }

            return true;
        }

        private void Feedback_Executed(object arg)
        {
            try
            {
                Process.Start(Uri.EscapeUriString(Resources.AuthorSiteUrl));
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
        }

        private void Donate_Executed(object arg)
        {
            try
            {
                Process.Start("https://money.yandex.ru/embed/shop.xml?account=410012283941753&quickpay=shop&payment-type-choice=on&writer=seller&targets=%D0%9F%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%BA%D0%B0+%D0%B0%D0%B2%D1%82%D0%BE%D1%80%D0%B0&targets-hint=&default-sum=100&button-text=03&comment=on&hint=%D0%92%D0%B0%D1%88+%D0%BA%D0%BE%D0%BC%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%80%D0%B8%D0%B9");
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
        }

        private void About_Executed(object arg)
        {
            DocList.Add(new AboutViewModel());
        }

        void MainViewModel_CurrentChanged(object sender, EventArgs e)
        {
            ActiveDocument = CollectionViewSource.GetDefaultView(DocList).CurrentItem as QDocument;
        }

        void DocList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (WorkspaceViewModel item in e.NewItems)
                    {
                        item.Error += ShowError;
                        item.NewItem += Item_NewDoc;
                        item.Closed += Item_Closed;
                    }
                    CollectionViewSource.GetDefaultView(DocList).MoveCurrentToLast();
                    CheckSaveAllCanBeExecuted(this, EventArgs.Empty);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (WorkspaceViewModel item in e.OldItems)
                    {
                        item.Error -= ShowError;
                        item.NewItem -= Item_NewDoc;
                        item.Closed -= Item_Closed;
                    }
                    CheckSaveAllCanBeExecuted(this, EventArgs.Empty);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;

                default:
                    break;
            }
        }

        void Item_NewDoc(WorkspaceViewModel doc)
        {
            DocList.Add(doc);
        }

        void Item_Closed(WorkspaceViewModel doc)
        {
            doc.Dispose();
            DocList.Remove(doc);
        }

        void CheckSaveAllCanBeExecuted(object sender, EventArgs e)
        {
            SaveAll.CanBeExecuted = DocList.Count > 0;
        }

        /// <summary>
        /// Новый
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DocList.Add(new NewViewModel(_storageContextViewModel));
        }

        /// <summary>
        /// Открыть существующий пакет
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        private void OpenRecent_Executed(object arg)
        {
            OpenFile(arg.ToString());
        }

        /// <summary>
        /// Открытие существующего файла
        /// </summary>
        /// <param name="path">Имя файла</param>
        /// <param name="fileStream">Открытый для чтения файл</param>
        internal void OpenFile(string path, string search = null, string overridePath = null, Action onSuccess = null)
        {
            var savingPath = overridePath ?? path;

            Task<QDocument> loader() => Task.Run(() =>
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);

                    // Раньше было read = false
                    // Но из-за этого при каждом открытии, даже если файл не изменялся, менялась дата его изменения
                    var doc = SIDocument.Load(stream);

                    var docViewModel = new QDocument(doc, _storageContextViewModel)
                    {
                        Path = savingPath,
                        FileName = Path.GetFileNameWithoutExtension(savingPath)
                    };

                    if (search != null)
                        docViewModel.SearchText = search;

                    if (overridePath != null)
                    {
                        docViewModel.OverridePath = overridePath;
                        docViewModel.OriginalPath = path;
                        docViewModel.Changed = true;
                    }

                    return docViewModel;
                }
                catch (Exception exc)
                {
                    if (stream != null)
                        stream.Dispose();

                    if (exc is FileNotFoundException)
                        AppSettings.Default.History.Remove(path);

                    if (exc is UnauthorizedAccessException && (new FileInfo(path).Attributes & FileAttributes.ReadOnly) > 0)
                    {
                        throw new Exception(Resources.FileIsReadOnly);
                    }

                    throw exc;
                }
            });

            DocList.Add(new DocumentLoaderViewModel(path, loader, () =>
            {
                AppSettings.Default.History.Add(savingPath);

                onSuccess?.Invoke();
            }));
        }

        private void ImportTxt_Executed(object arg)
        {
            var model = new ImportTextViewModel(arg, _storageContextViewModel);
            DocList.Add(model);
            model.Start();
        }

        /// <summary>
        /// Импортировать текст из буфера обмена
        /// </summary>
        /// <param name="arg"></param>
        private void ImportClipboardTxt_Executed(object arg)
        {
            var model = new ImportTextViewModel(typeof(Clipboard), _storageContextViewModel);
            DocList.Add(model);
            model.Start();
        }

        private async void ImportXml_Executed(object arg)
        {
            var file = PlatformManager.Instance.ShowImportXmlUI();
            if (file == null)
                return;

            try
            {
                using (var stream = File.OpenRead(file))
                {
                    var doc = await SIDocument.LoadXml(stream);

                    var docViewModel = new QDocument(doc, _storageContextViewModel) { Path = "", Changed = true, FileName = Path.GetFileNameWithoutExtension(file) };

                    LoadMediaFromFolder(docViewModel, Path.GetDirectoryName(file));

                    DocList.Add(docViewModel);
                }
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
        }

        private void LoadMediaFromFolder(QDocument document, string folder)
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
                                    continue;

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
        /// Импортировать пакет из Базы вопросов
        /// </summary>
        /// <param name="arg"></param>
        private void ImportBase_Executed(object arg)
        {
            DocList.Add(new ImportDBStorageViewModel(_storageContextViewModel));
        }

        private void ImportFromSIStore_Executed(object arg)
        {
            DocList.Add(new ImportSIStorageViewModel(_storageContextViewModel));
        }

        private async void SaveAll_Executed(object arg)
        {
            foreach (var item in DocList.ToArray())
            {
                try
                {
                    await item.SaveIfNeeded(false);
                }
                catch (Exception exc)
                {
                    ShowError(exc);
                }
            }
        }        

        public static void ShowError(Exception exc)
        {
            var message = exc.Message;
            if (message.Length > 1000)
                message = message.Substring(0, 1000) + "…";

            PlatformManager.Instance.ShowExclamationMessage(message);
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
