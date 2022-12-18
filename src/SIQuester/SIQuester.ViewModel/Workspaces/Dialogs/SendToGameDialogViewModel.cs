using SIQuester.ViewModel.PlatformSpecific;
using System.Net;
using System.Text;
using System.Windows.Input;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

public sealed class SendToGameDialogViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;

    public override string Header => "Отправка пакета в компьютерную игру";

    private string _comment = "";

    public string Comment
    {
        get { return _comment; }
        set { _comment = value; OnPropertyChanged(); }
    }

    public ICommand Send { get; private set; }

    public SendToGameDialogViewModel(QDocument document)
    {
        _document = document;

        Send = new SimpleCommand(Send_Executed);
    }

    private async void Send_Executed(object arg)
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
                ErrorMessage = "Размер файла превышает 100 Мб!";
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
                PlatformManager.Instance.Inform("Пакет успешно отправлен!");
            }
            else
            {
                ErrorMessage = "Не удалось отправить пакет: " +
                    (response.StatusCode == HttpStatusCode.Gone ?
                    "в настоящий момент на сервере нет места для принятия новых пакетов" :
                    await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }
}
