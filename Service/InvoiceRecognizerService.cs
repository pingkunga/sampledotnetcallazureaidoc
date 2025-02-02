using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class InvoiceRecognizerService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public InvoiceRecognizerService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _endpoint = configuration["DocumentIntelligentAPI:Endpoint"];
        _apiKey = configuration["DocumentIntelligentAPI:ApiKey"];
    }

    public async Task<JsonDocument> AnalyzeDocumentAsync(Stream documentStream)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_endpoint}/formrecognizer/documentModels/prebuilt-receipt:analyze?api-version=2023-07-31"),
            Content = new StreamContent(documentStream)
        };

        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Check if the response contains the header `apim-request-id`
        if (response.Headers.Contains("apim-request-id"))
        {
            // Get the value of the header
            var requestId = response.Headers.GetValues("apim-request-id").FirstOrDefault();
            
            var resultRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_endpoint}/formrecognizer/documentModels/prebuilt-receipt/analyzeResults/{requestId}?api-version=2023-07-31")
            };
            resultRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            // Wait for the response
            while (true)
            {
                var resultResponse = await _httpClient.SendAsync(resultRequest);
                resultResponse.EnsureSuccessStatusCode();
                var resultJson = await resultResponse.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(resultJson);
                if (result.RootElement.GetProperty("status").GetString() == "succeeded")
                {
                    return result;
                }
                await Task.Delay(1000);
            }
        }

        return null;
    }
}