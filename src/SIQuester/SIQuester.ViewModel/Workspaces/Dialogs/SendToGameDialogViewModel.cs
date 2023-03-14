using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.Net;
using System.Text;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

/// <summary>
/// Represents a package send to game view model.
/// </summary>
public sealed class SendToGameDialogViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;

    public override string Header => Resources.SendToGame;

    private string _comment = "";

    public string Comment
    {
        get => _comment;
        set { _comment = value; OnPropertyChanged(); }
    }

    public ICommand Send { get; private set; }

    public SendToGameDialogViewModel(QDocument document)
    {
        _document = document;

        Send = new SimpleCommand(Send_Executed);
    }

    private async void Send_Executed(object? arg)
    {
        try
        {
            using var stream = new MemoryStream();
            using (var tempDoc = _document.Document.SaveAs(stream, false))
            {
                tempDoc.FinalizeSave();
            }

            var data = stream.ToArray();

            if (data.Length > 100 * 1024 * 1024)
            {
                ErrorMessage = Resources.GamePackageTooLarge;
                return;
            }

            using var ms = new MemoryStream(data);
            var url = "https://vladimirkhil.com/api/si/AddFile";
            var bytesContent = new StreamContent(ms, 262144);
            bytesContent.Headers.Add("message", Convert.ToBase64String(Encoding.UTF8.GetBytes(Comment)));

            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);
            using var formData = new MultipartFormDataContent
            {
                { bytesContent, "file", Path.GetFileName(_document.Path) }
            };

            using var response = await client.PostAsync(url, formData);
            if (response.IsSuccessStatusCode)
            {
                PlatformManager.Instance.Inform(Resources.SendPackageSuccess);
            }
            else
            {
                ErrorMessage = Resources.SendPackageErrorHeader + ": " +
                    (response.StatusCode == HttpStatusCode.Gone ?
                    Resources.PackageServerFull :
                    await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }
}
