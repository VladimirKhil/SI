using SIQuester.ViewModel.Contracts;
using System.Xml;

namespace SIQuester.ViewModel.Services;

/// <inheritdoc cref="IChgkDbClient" />
internal sealed class ChgkDbClient : IChgkDbClient
{
    private readonly HttpClient _httpClient;

    public Uri? ServiceUri => _httpClient.BaseAddress;

    public ChgkDbClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<XmlDocument> GetXmlAsync(string name, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"tour/{name}/xml", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var xmlDocument = new XmlDocument();

        using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            xmlDocument.Load(stream);
        }

        return xmlDocument;
    }
}
