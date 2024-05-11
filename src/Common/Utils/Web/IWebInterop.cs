namespace Utils.Web;

public interface IWebInterop
{
    event Action<string> SendJsonMessage;

    void OnMessage(string webMessageAsJson) { }
}
