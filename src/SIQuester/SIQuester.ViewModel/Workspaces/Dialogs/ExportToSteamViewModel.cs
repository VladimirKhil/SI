﻿using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using Steamworks;
using System.Text;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

public sealed class ExportToSteamViewModel : WorkspaceViewModel
{
    private readonly bool _initialized = false;

    private readonly QDocument _document;

    public override string Header => Resources.ExportToSteam;

    public string Title => _document.Document.Package.Name;

    public string Description { get; set; }

    public string Tags => string.Join(", ", _document.Document.Package.Tags);

    private int _progress;

    public int Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
    }

    private string _status = "";

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    private string _itemLink = "";

    public string ItemLink
    {
        get => _itemLink;
        set
        {
            if (_itemLink != value)
            {
                _itemLink = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand Upload { get; }
    
    private readonly CallResult<CreateItemResult_t> _createItemCallback;
    private readonly CallResult<SubmitItemUpdateResult_t> _submitItemCallback;
    private readonly CallResult<SubmitItemUpdateResult_t> _submitPreviewCallback;

    private readonly System.Timers.Timer _callbackTimer;
    private System.Timers.Timer? _timer;

    private bool _isUploading = false;

    public bool IsUploading
    {
        get => _isUploading;
        set
        {
            if (_isUploading != value)
            {
                _isUploading = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _tempFolder;

    public ExportToSteamViewModel(QDocument document)
    {
        var initResult = SteamAPI.InitEx(out var error);

        if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
        {
            throw new Exception($"{initResult}: {error}");
        }

        _initialized = true;
        _document = document;

        var preview = new PreviewViewModel(_document.Document);

        var description = new StringBuilder();

        description.Append($"{Resources.QuestionCount}: {preview.QuestionCount}").AppendLine();

        foreach (var content in preview.Content)
        {
            description.Append($"{LocalizeContentType(content.Key)}: {content.Value}").AppendLine();
        }

        description.Append($"{Resources.Tags}: {string.Join(", ", _document.Package.Tags)}").AppendLine().AppendLine();
        description.Append(_document.Document.Package.Info.Comments.Text).AppendLine().AppendLine();

        foreach (var round in _document.Document.Package.Rounds)
        {
            description.Append("[h1]").Append(round.Name).Append("[/h1]").AppendLine();

            foreach (var theme in round.Themes)
            {
                description.AppendLine(theme.Name);
            }

            description.AppendLine();
        }

        Description = description.ToString();

        Upload = new SimpleCommand(Upload_Executed);

        _createItemCallback = CallResult<CreateItemResult_t>.Create(OnCreateItem);
        _submitItemCallback = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItem);
        _submitPreviewCallback = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitPreview);

        _callbackTimer = new System.Timers.Timer(100);
        _callbackTimer.Elapsed += (s, e) => SteamAPI.RunCallbacks();
        _callbackTimer.Start();
    }

    private static string LocalizeContentType(string key) => key switch
    {
        ContentTypes.Text => Resources.Text,
        ContentTypes.Image => Resources.Image,
        ContentTypes.Audio => Resources.Audio,
        ContentTypes.Video => Resources.Video,
        _ => key
    };

    private void Upload_Executed(object? arg)
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Status = Resources.SteamNotRunning;
            return;
        }

        Status = Resources.StartingUpload;
        Progress = 0;
        IsUploading = true;

        var call = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        _createItemCallback.Set(call);
    }

    private void OnCreateItem(CreateItemResult_t param, bool bIOFailure)
    {
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Status = $"{Resources.WorkshopItemCreationError}: {param.m_eResult}";
            Progress = 0;
            IsUploading = false;
            return;
        }

        if (!SteamAPI.IsSteamRunning())
        {
            Status = Resources.SteamNotRunning;
            return;
        }

        var publishedFileId = param.m_nPublishedFileId;

        try
        {
            var updateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), publishedFileId);

            if (updateHandle == UGCUpdateHandle_t.Invalid)
            {
                Status = Resources.InvalidUpdateHandle;
                Progress = 0;
                IsUploading = false;
                return;
            }

            _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempFolder);
            
            var contentFolder = Path.Combine(_tempFolder, "content");
            Directory.CreateDirectory(contentFolder);

            File.Copy(_document.Path, Path.Combine(contentFolder, "package.siq"), true);

            var language = AppSettings.Default.Language == "ru-RU" ? "Russian" : "English";

            SteamUGC.SetItemContent(updateHandle, contentFolder);
            SteamUGC.SetItemTitle(updateHandle, Title);
            SteamUGC.SetItemDescription(updateHandle, Description);
            SteamUGC.SetItemTags(updateHandle, new[] { language });
            SteamUGC.AddItemKeyValueTag(updateHandle, "Difficulty", "Intermediate"); // For test purposes
            SteamUGC.SetItemUpdateLanguage(updateHandle, language.ToLower());
            SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
            
            var call = SteamUGC.SubmitItemUpdate(updateHandle, Resources.InitialUpload);
            _submitItemCallback.Set(call);

            _timer = new System.Timers.Timer(100);

            _timer.Elapsed += (s, e) =>
            {
                var status = SteamUGC.GetItemUpdateProgress(updateHandle, out ulong bytesProcessed, out ulong bytesTotal);

                if (status == EItemUpdateStatus.k_EItemUpdateStatusInvalid)
                {
                    _timer.Stop();
                    IsUploading = false;
                    return;
                }

                if (bytesTotal > 0)
                {
                    float progress = (float)bytesProcessed / bytesTotal;
                    Progress = (int)(100 * progress);
                }
            };

            _timer.Start();
        }
        catch (Exception ex)
        {
            Status = $"{Resources.UploadError}: {ex.Message}";
            Progress = 0;
            IsUploading = false;
        }
    }

    private void OnSubmitItem(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        _timer?.Stop();
        IsUploading = false;

        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Status = $"{Resources.UploadError}: {param.m_eResult}";
            Progress = 0;
            return;
        }

        ItemLink = $"https://steamcommunity.com/sharedfiles/filedetails/?id={param.m_nPublishedFileId}";
        Status = Resources.UploadSuccess;

        var logo = _document.Package.Logo;

        if (logo != null && _tempFolder != null && logo.Uri != null && File.Exists(logo.Uri))
        {
            var updateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), param.m_nPublishedFileId);

            var logoFile = Path.Combine(_tempFolder, "preview.png");
            File.Copy(logo.Uri, logoFile, true);

            SteamUGC.SetItemPreview(updateHandle, logoFile);

            var call = SteamUGC.SubmitItemUpdate(updateHandle, Resources.PreviewUpload);
            _submitPreviewCallback.Set(call);

            Status += ". " + Resources.PreviewUpload;
        }
    }

    private void OnSubmitPreview(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        Status = Resources.UploadSuccess;
        
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            PlatformSpecific.PlatformManager.Instance.ShowExclamationMessage($"{Resources.PreviewUploadError}: {param.m_eResult}");
            return;
        }

        Status += ". " + Resources.PreviewUploadSuccess;
    }

    protected override void Dispose(bool disposing)
    {
        if (_initialized)
        {
            SteamAPI.Shutdown();
        }

        _callbackTimer.Dispose();
        _timer?.Dispose();

        base.Dispose(disposing);
    }
}
