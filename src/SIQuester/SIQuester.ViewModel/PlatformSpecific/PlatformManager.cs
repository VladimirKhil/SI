using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace SIQuester.ViewModel.PlatformSpecific;

/// <summary>
/// Логика, различающаяся на разных платформах
/// </summary>
public abstract class PlatformManager
{
    public static PlatformManager Instance;

    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Gets well-known font family names.
    /// </summary>
    public abstract string[] FontFamilies { get; }

    protected PlatformManager()
    {
        Instance = this;
    }

    public abstract Tuple<int, int, int>? GetCurrentItemSelectionArea();

    /// <summary>
    /// Creates document preview image and saves it to file.
    /// </summary>
    /// <param name="document">Document to process.</param>
    public abstract void CreatePreview(SIDocument document);

    public abstract string[]? ShowOpenUI();

    public abstract string[]? ShowMediaOpenUI(string mediaCategory, bool allowAnyFile);

    public abstract bool ShowSaveUI(
        string? title,
        string defaultExtension,
        Dictionary<string, string>? filter,
        [NotNullWhen(true)] ref string? filename);

    public abstract bool ShowExportUI(
        string title,
        Dictionary<string, string> filter,
        [NotNullWhen(true)] ref string? filename,
        ref int filterIndex,
        out Encoding encoding,
        out bool start);

    public abstract string? ShowImportUI(string fileExtension, string fileFilter);

    public abstract string? SelectSearchFolder();

    public abstract IMedia PrepareMedia(IMedia media, string type);

    public abstract void ClearMedia(IEnumerable<string> media);

    public abstract string? AskText(string title, bool multiline = false);

    public abstract IEnumerable<string>? AskTags(ItemsViewModel<string> tags);

    public abstract IFlowDocumentWrapper BuildDocument(SIDocument doc, ExportFormats format);

    public abstract void ExportTable(SIDocument doc, string filename);

    public abstract void ShowHelp();

    public abstract void AddToRecentCategory(string fileName);

    public abstract void ShowErrorMessage(string message);

    public abstract void ShowExclamationMessage(string message);

    public abstract void ShowSelectOptionDialog(string message, params UserOption[] options);

    public abstract void Inform(string message, bool exclamation = false);

    public abstract bool Confirm(string message);

    public abstract bool? ConfirmWithCancel(string message);

    public abstract bool ConfirmExclWithWindow(string message);

    public abstract void Exit();

    public abstract string CompressImage(string imageUri);

    public abstract void CopyInfo(object info);

    public abstract Dictionary<string, JsonElement>? PasteInfo();

    public abstract IDisposable ShowProgressDialog();
}
