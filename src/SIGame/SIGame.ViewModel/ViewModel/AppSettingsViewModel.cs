using Notions;
using SICore;
using SIData;
using SIGame.ViewModel.Properties;
using System.IO.Compression;
using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Модель отображения редактируемых настроек
/// </summary>
public sealed class AppSettingsViewModel : ViewModel<AppSettings>
{
    /// <summary>
    /// Имя файла общих настроек
    /// </summary>
    private const string CommonPart = "app.config";

    /// <summary>
    /// Имя файла персональных настроек
    /// </summary>
    private const string UserPart = "user.config";

    private string _oldLogsFolder = null;

    /// <summary>
    /// Принять изменения параметров
    /// </summary>
    public ICommand Apply { get; private set; }

    /// <summary>
    /// Установить параметры в значения по умолчанию
    /// </summary>
    public ICommand SetDefault { get; private set; }

    /// <summary>
    /// Переместить папку логов
    /// </summary>
    public ICommand MoveLogs { get; private set; }

    /// <summary>
    /// Экспорт настроек
    /// </summary>
    public ICommand Export { get; private set; }

    /// <summary>
    /// Импорт настроек
    /// </summary>
    public ICommand Import { get; private set; }

    internal event Action Close;

    public TimeSettingsViewModel TimeSettings { get; private set; }

    private bool _isEditable = true;

    public bool IsEditable
    {
        get => _isEditable;
        set { if (_isEditable != value) { _isEditable = value; OnPropertyChanged(); } }
    }

    public GameModes GameMode
    {
        get => _model.GameMode;
        set
        {
            if (_model.GameMode != value)
            {
                _model.GameMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameModeHint));
            }
        }
    }

    public string GameModeHint => _model.GameMode == GameModes.Tv ? Resources.GameModes_Tv_Hint : Resources.GameModes_Sport_Hint;

    public ThemeSettingsViewModel ThemeSettings { get; private set; }

    public AppSettingsViewModel(AppSettings settings)
        : base(settings)
    {
        ThemeSettings = new ThemeSettingsViewModel(settings.ThemeSettings);
    }

    protected override void Initialize()
    {
        base.Initialize();

        Apply = new CustomCommand(Apply_Executed);
        SetDefault = new CustomCommand(SetDefault_Executed);
        MoveLogs = new CustomCommand(MoveLogs_Executed);
        Export = new CustomCommand(Export_Executed);
        Import = new CustomCommand(Import_Executed);

        TimeSettings = new TimeSettingsViewModel(_model.TimeSettings);
    }

    private async void Import_Executed(object? arg)
    {
        var settingsFile = PlatformSpecific.PlatformManager.Instance.SelectSettingsForImport();

        if (settingsFile == null)
        {
            return;
        }

        try
        {
            using (var fileStream = File.OpenRead(settingsFile))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                var commonSettingsPart = package.GetEntry(CommonPart);

                using (var stream = commonSettingsPart.Open())
                {
                    CommonSettings.Default.LoadFrom(stream);
                }

                foreach (var part in package.Entries)
                {
                    var partUri = part.Name;

                    if (partUri.StartsWith("Human"))
                    {
                        var name = Path.GetFileNameWithoutExtension(partUri[(partUri.IndexOf('_') + 1)..]);
                        var item = CommonSettings.Default.Humans2.FirstOrDefault(human => human.Name == name);

                        if (item != null)
                        {
                            await SetPictureAsync(part, partUri, item);
                        }
                    }
                    else if (partUri.StartsWith("Computer"))
                    {
                        var name = Path.GetFileNameWithoutExtension(partUri[(partUri.IndexOf('_') + 1)..]);
                        var item = CommonSettings.Default.CompPlayers2.FirstOrDefault(comp => comp.Name == name);

                        if (item != null)
                        {
                            await SetPictureAsync(part, partUri, item);
                        }
                    }
                }

                var userSettingsPart = package.GetEntry(UserPart);

                using (var stream = userSettingsPart.Open())
                {
                    UserSettings.Default.LoadFrom(stream);
                }
            }

            PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.ImportOk, PlatformSpecific.MessageType.OK);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.Message, PlatformSpecific.MessageType.Error);
        }
    }

    private async void Export_Executed(object? arg)
    {
        var exportFile = PlatformSpecific.PlatformManager.Instance.SelectSettingsForExport();

        if (exportFile == null)
        {
            return;
        }

        try
        {
            using (var fileStream = File.Create(exportFile))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var commonSettingsPart = package.CreateEntry(CommonPart);

                using (var stream = commonSettingsPart.Open())
                {
                    CommonSettings.Default.Save(stream);
                }

                foreach (var item in CommonSettings.Default.Humans2)
                {
                    await AddImageAsync(package, "Human", item);
                }

                foreach (var item in CommonSettings.Default.CompPlayers2)
                {
                    await AddImageAsync(package, "Computer", item);
                }

                foreach (var item in CommonSettings.Default.CompShowmans2)
                {
                    await AddImageAsync(package, "Showman", item);
                }

                var userSettingsPart = package.CreateEntry(UserPart);

                using (var stream = userSettingsPart.Open())
                {
                    UserSettings.Default.Save(stream);
                }
            }

            PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.ExportOk, PlatformSpecific.MessageType.OK);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.ToString(), PlatformSpecific.MessageType.Warning);

            if (File.Exists(exportFile))
            {
                File.Delete(exportFile);
            }
        }
    }

    private void MoveLogs_Executed(object? arg)
    {
        var path = PlatformSpecific.PlatformManager.Instance.SelectLogsFolder(_model.LogsFolder);

        if (path != null)
        {
            MoveLogsTo(path);
        }
    }

    private void SetDefault_Executed(object? arg)
    {
        _model.ReadingSpeed = AppSettingsCore.DefaultReadingSpeed;
        _model.MultimediaPort = AppSettingsCore.DefaultMultimediaPort;
        _model.TranslateGameToChat = false;
        _model.FalseStart = AppSettingsCore.DefaultFalseStart;
        _model.HintShowman = AppSettingsCore.DefaultHintShowman;
        _model.Oral = AppSettingsCore.DefaultOral;
        _model.OralPlayersActions = AppSettingsCore.DefaultOralPlayersActions;
        _model.IgnoreWrong = AppSettingsCore.DefaultIgnoreWrong;
        _model.DisplaySources = AppSettingsCore.DefaultDisplaySources;
        _model.GameMode = AppSettingsCore.DefaultGameMode;
        ThemeSettings.Reset();
        _model.GameButtonKey2 = AppSettings.DefaultGameButtonKey2;
        _model.RandomRoundsCount = AppSettingsCore.DefaultRandomRoundsCount;
        _model.RandomThemesCount = AppSettingsCore.DefaultRandomThemesCount;
        _model.RandomQuestionsBasePrice = AppSettingsCore.DefaultRandomQuestionsBasePrice;
        _model.UseApellations = AppSettingsCore.DefaultUseApellations;

        foreach (var item in TimeSettings.All)
        {
            item.Value.Value = item.Value.DefaultValue;
        }

        PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.SettingsReset, PlatformSpecific.MessageType.OK);
    }

    private void Apply_Executed(object? arg)
    {
        UserSettings.Default.GameSettings.AppSettings = _model;

        if (_oldLogsFolder != null && _oldLogsFolder != _model.LogsFolder)
        {
            try
            {
                MoveData(_oldLogsFolder, _model.LogsFolder);
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.ShowMessage(
                    $"{Resources.LogsMovingError}: {exc.Message}",
                    PlatformSpecific.MessageType.Error);
            }
        }

        Close?.Invoke();
    }

    private static async Task SetPictureAsync(ZipArchiveEntry part, string partUri, Account item)
    {
        var ext = Path.GetExtension(partUri);

        if (Directory.Exists(Global.PhotoUri))
        {
            var localPath = Path.Combine(Global.PhotoUri, Path.GetFileNameWithoutExtension(partUri).Translit() + ext);

            using (var stream = part.Open())
            {
                using var fileStream = File.Create(localPath);
                await stream.CopyToAsync(fileStream);
            }

            item.Picture = localPath;
        }
    }

    private static async Task AddImageAsync(ZipArchive package, string category, Account account)
    {
        if (!account.CanBeDeleted || string.IsNullOrWhiteSpace(account.Picture))
        {
            return;
        }

        if (!Uri.TryCreate(account.Picture, UriKind.RelativeOrAbsolute, out var uri))
        {
            return;
        }

        if (!uri.IsAbsoluteUri || uri.Scheme != "file" || !File.Exists(uri.LocalPath))
        {
            return;
        }

        var partUri = string.Format("{0}_{1}{2}", category, account.Name, Path.GetExtension(account.Picture));
        var part = package.CreateEntry(partUri);

        using var stream = part.Open();
        using var fileStream = File.Open(account.Picture, FileMode.Open);
        await fileStream.CopyToAsync(stream);
    }

    private void MoveLogsTo(string newPath)
    {
        if (_model.LogsFolder != newPath)
        {
            _oldLogsFolder ??= _model.LogsFolder;
            _model.LogsFolder = newPath;
        }
    }

    private static void MoveData(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        MoveData(new DirectoryInfo(source), destination);
    }

    private static void MoveData(DirectoryInfo source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var dir in source.GetDirectories())
        {
            MoveData(dir, Path.Combine(destination, dir.Name));

            if (dir.GetFileSystemInfos().Length == 0)
            {
                Directory.Delete(dir.FullName);
            }
        }

        foreach (var file in source.GetFiles())
        {
            var targetFile = Path.Combine(destination, file.Name);

            if (!File.Exists(targetFile))
            {
                File.Move(file.FullName, targetFile);
            }
        }
    }
}
