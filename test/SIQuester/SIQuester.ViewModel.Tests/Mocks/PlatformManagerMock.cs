using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace SIQuester.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of PlatformManager that does nothing.
/// </summary>
internal sealed class PlatformManagerMock : PlatformManager
{
    public override string[] FontFamilies => Array.Empty<string>();

    public override Tuple<int, int, int>? GetCurrentItemSelectionArea() => null;

    public override void CreatePreview(SIDocument document) { }

    public override string[]? ShowOpenUI() => null;

    public override string[]? ShowMediaOpenUI(string mediaCategory, bool allowAnyFile, bool multiselect = true) => null;

    public override bool ShowSaveUI(
        string? title,
        string defaultExtension,
        Dictionary<string, string>? filter,
        [NotNullWhen(true)] ref string? filename) => false;

    public override bool ShowExportUI(
        string title,
        Dictionary<string, string> filter,
        [NotNullWhen(true)] ref string? filename,
        ref int filterIndex,
        out Encoding encoding,
        out bool start)
    {
        encoding = Encoding.UTF8;
        start = false;
        return false;
    }

    public override string? ShowImportUI(string fileExtension, string fileFilter) => null;

    public override string? SelectSearchFolder() => null;

    public override IMedia PrepareMedia(IMedia media, string type) => media;

    public override void ClearMedia(IEnumerable<string> media) { }

    public override string? AskText(string title, bool multiline = false) => null;

    public override IEnumerable<string>? AskTags(ItemsViewModel<string> tags) => null;

    public override IFlowDocumentWrapper BuildDocument(SIDocument doc, ExportFormats format) =>
        throw new NotSupportedException();

    public override void ExportTable(SIDocument doc, string filename) { }

    public override void ShowHelp() { }

    public override void AddToRecentCategory(string fileName) { }

    public override void ShowErrorMessage(string message) { }

    public override void ShowExclamationMessage(string message) { }

    public override void ShowSelectOptionDialog(string message, params UserOption[] options) { }

    public override void Inform(string message, bool exclamation = false) { }

    public override bool Confirm(string message) => true;

    public override bool? ConfirmWithCancel(string message) => true;

    public override bool ConfirmExclamationWithWindow(string message) => true;

    public override void Exit() { }

    public override string CompressImage(string imageUri) => imageUri;

    public override void CopyInfo(object info) { }

    public override Dictionary<string, JsonElement>? PasteInfo() => null;

    public override IDisposable ShowProgressDialog() => new NullDisposable();

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
