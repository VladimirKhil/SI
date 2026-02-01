using SIPackages.Core;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Properties;
using Steamworks;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

public sealed class ExportToSteamViewModel : WorkspaceViewModel
{
    private static readonly Dictionary<string, string> _languages = new()
    {
        { "ru-RU", "Russian" },
        { "sr-RS", "Serbian" },
        { "other", "Other" }
    };

    private readonly bool _initialized = false;

    private readonly QDocument _document;

    public override string Header => Resources.ExportToSteam;

    public string Title => _document.Document.Package.Name;

    private string _desription = "";

    public string Description
    {
        get => _desription;
        set
        {
            if (_desription != value)
            {
                _desription = value;
                OnPropertyChanged();
            }
        }
    }

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

    /// <summary>
    /// Workshop items created by the user.
    /// </summary>
    public ObservableCollection<WorkshopItemViewModel> UserItems { get; } = new();

    private WorkshopItemViewModel? _selectedItem;

    public WorkshopItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();

                if (_selectedItem != null)
                {
                    Description = _selectedItem.Description;
                }
            }
        }
    }

    public ICommand Upload { get; }
    
    private readonly CallResult<SteamUGCQueryCompleted_t> _getUserItemsCallback;
    private readonly CallResult<CreateItemResult_t> _createItemCallback;
    private readonly CallResult<SubmitItemUpdateResult_t> _submitItemCallback;
    private readonly CallResult<SubmitItemUpdateResult_t> _submitPreviewCallback;

    private UGCQueryHandle_t _userItemsQuery;

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

    private readonly Dictionary<string, object> _metadata = new();

    public bool ShowMissingPreviewWarning => _document.Package.Model.Logo.Length == 0;

    public ExportToSteamViewModel(QDocument document)
    {
        var initResult = SteamAPI.InitEx(out var error);

        if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
        {
            if (initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient)
            {
                throw new Exception(Resources.SteamNotRunning);
            }

            throw new Exception($"{initResult}: {error}");
        }

        _initialized = true;
        _document = document;

        var preview = new PreviewViewModel(_document.Document);
        Description = BuildDescription(preview);

        _metadata["questionCount"] = preview.QuestionCount;
        _metadata["content"] = preview.Content;

        Upload = new SimpleCommand(Upload_Executed);

        _getUserItemsCallback = CallResult<SteamUGCQueryCompleted_t>.Create(OnGetUserItems);
        _createItemCallback = CallResult<CreateItemResult_t>.Create(OnCreateItem);
        _submitItemCallback = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItem);
        _submitPreviewCallback = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitPreview);

        _callbackTimer = new System.Timers.Timer(100);
        _callbackTimer.Elapsed += (s, e) => SteamAPI.RunCallbacks();
        _callbackTimer.Start();

        LoadUserItems();
    }

    private string BuildDescription(PreviewViewModel preview)
    {
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

        return description.ToString();
    }

    private void LoadUserItems()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Status = Resources.SteamNotRunning;
            return;
        }
        
        // Create a query for the current user's published items
        _userItemsQuery = SteamUGC.CreateQueryUserUGCRequest(
            SteamUser.GetSteamID().GetAccountID(),
            EUserUGCList.k_EUserUGCList_Published,
            EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
            EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc,
            SteamUtils.GetAppID(),
            SteamUtils.GetAppID(),
            1 // page number
        );

        if (_userItemsQuery == UGCQueryHandle_t.Invalid)
        {
            Status = Resources.WorkshopItemsLoadError;
            return;
        }

        // Request additional details for each item
        SteamUGC.SetReturnMetadata(_userItemsQuery, true);
        SteamUGC.SetReturnLongDescription(_userItemsQuery, true);
        SteamUGC.SetReturnTotalOnly(_userItemsQuery, false);

        var call = SteamUGC.SendQueryUGCRequest(_userItemsQuery);
        _getUserItemsCallback.Set(call);
    }

    private void OnGetUserItems(SteamUGCQueryCompleted_t param, bool bIOFailure)
    {
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Status = $"{Resources.WorkshopItemsLoadError}: {param.m_eResult}";
            return;
        }

        var userItems = new List<WorkshopItemViewModel>();

        for (uint i = 0; i < param.m_unNumResultsReturned; i++)
        {
            if (SteamUGC.GetQueryUGCResult(param.m_handle, i, out SteamUGCDetails_t details))
            {
                var item = new WorkshopItemViewModel(details.m_nPublishedFileId)
                {
                    Title = details.m_rgchTitle,
                    Description = details.m_rgchDescription,
                    Tags = details.m_rgchTags
                };

                userItems.Add(item);
            }
        }
        
        // Release the query handle
        SteamUGC.ReleaseQueryUGCRequest(param.m_handle);

        UI.Execute(
            () =>
            {
                UserItems.Clear();
            
                foreach (var item in userItems)
                {
                    UserItems.Add(item);
                }
            },
            exc => OnError(exc));

        // TODO: use param.m_unTotalMatchingResults to query next pages if needed
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

        if (SelectedItem != null)
        {
            // Update existing item
            UpdateExistingItem(SelectedItem.PublishedFileId, Resources.WorkshopItemUpdate);
        }
        else
        {
            // Create new item
            var call = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            _createItemCallback.Set(call);
        }
    }

    private void UpdateExistingItem(PublishedFileId_t publishedFileId, string message)
    {
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

            PrepareItemUpdate(updateHandle);
            
            var call = SteamUGC.SubmitItemUpdate(updateHandle, message);
            _submitItemCallback.Set(call);

            StartProgressTimer(updateHandle);
        }
        catch (Exception ex)
        {
            Status = $"{Resources.UploadError}: {ex.Message}";
            Progress = 0;
            IsUploading = false;
        }
    }

    private void PrepareItemUpdate(UGCUpdateHandle_t updateHandle)
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempFolder);
        
        var contentFolder = Path.Combine(_tempFolder, "content");
        Directory.CreateDirectory(contentFolder);

        File.Copy(_document.Path, Path.Combine(contentFolder, "package.siq"), true);

        var tags = new List<string>();

        var languageCode = _document.Package.Model.Language;
        var localizeTags = true;

        if (!_languages.TryGetValue(languageCode, out var languageName))
        {
            languageName = "English";
            localizeTags = false;
        }

        if (!string.IsNullOrEmpty(languageCode))
        {
            tags.Add(languageName);
        }

        foreach (var tag in _document.Package.Tags)
        {
            if (!localizeTags || !TagsRepository.Instance.Translation.TryGetValue(tag, out var englishTag))
            {
                tags.Add(tag);
            }
            else
            {
                tags.Add(englishTag);
            }
        }

        SteamUGC.SetItemContent(updateHandle, contentFolder);
        SteamUGC.SetItemTitle(updateHandle, Title);
        SteamUGC.SetItemDescription(updateHandle, Description);
        SteamUGC.SetItemTags(updateHandle, tags);
        SteamUGC.SetItemMetadata(updateHandle, JsonSerializer.Serialize(_metadata));
        SteamUGC.SetItemUpdateLanguage(updateHandle, languageName.ToLower());
        SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
    }

    private void StartProgressTimer(UGCUpdateHandle_t updateHandle)
    {
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

    private void OnCreateItem(CreateItemResult_t param, bool bIOFailure)
    {
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Status = $"{Resources.WorkshopItemCreationError}: {GetUserMessage(param.m_eResult)}";
            Progress = 0;
            IsUploading = false;
            return;
        }

        if (!SteamAPI.IsSteamRunning())
        {
            Status = Resources.SteamNotRunning;
            IsUploading = false;
            return;
        }

        UpdateExistingItem(param.m_nPublishedFileId, Resources.InitialUpload);
    }

    private void OnSubmitItem(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        _timer?.Stop();
        IsUploading = false;

        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Status = $"{Resources.UploadError}: {GetUserMessage(param.m_eResult)}";
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

    private static string GetUserMessage(EResult m_eResult) => m_eResult switch
    {
        EResult.k_EResultFail => Resources.SteamResultFail,
        _ => m_eResult.ToString()
    };

    private void OnSubmitPreview(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        Status = Resources.UploadSuccess;
        
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            PlatformSpecific.PlatformManager.Instance.ShowExclamationMessage($"{Resources.PreviewUploadError}: {GetUserMessage(param.m_eResult)}");
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
