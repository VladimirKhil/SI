using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Model;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SIQuester.ViewModel.PlatformSpecific;

/// <summary>
/// Логика, различающаяся на разных платформах
/// </summary>
public abstract class PlatformManager
{
    internal static PlatformManager Instance;

    public IServiceProvider ServiceProvider { get; set; }

    protected PlatformManager()
    {
        Instance = this;
    }

    public abstract Tuple<int, int, int>? GetCurrentItemSelectionArea();

    public abstract string[]? ShowOpenUI();

    public abstract string[]? ShowMediaOpenUI(string mediaCategory);

    public abstract bool ShowSaveUI(
        string? title,
        string defaultExtension,
        Dictionary<string, string>? filter,
        [NotNullWhen(true)] ref string? filename);

    public abstract bool ShowExportUI(string title, Dictionary<string, string> filter, ref string filename, ref int filterIndex, out Encoding encoding, out bool start);

    public abstract string? ShowImportUI(string fileExtension, string fileFilter);

    public abstract string? SelectSearchFolder();

    public abstract IMedia PrepareMedia(IMedia media, string type);

    public abstract void ClearMedia(IEnumerable<string> media);

    public abstract string? AskText(string title, bool multiline = false);

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
}
