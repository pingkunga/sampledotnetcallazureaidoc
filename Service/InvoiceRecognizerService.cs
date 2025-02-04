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
    private readonly string _apiversion;

    public InvoiceRecognizerService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _endpoint =
            configuration["DocumentIntelligentAPI:Endpoint"]
            ?? throw new ArgumentNullException(
                nameof(configuration),
                "Endpoint configuration is missing."
            );
        _apiKey =
            configuration["DocumentIntelligentAPI:ApiKey"]
            ?? throw new ArgumentNullException(
                nameof(configuration),
                "ApiKey configuration is missing."
            );
        _apiversion =
            configuration["DocumentIntelligentAPI:ApiVersion"]
            ?? throw new ArgumentNullException(
                nameof(configuration),
                "ApiVersion configuration is missing."
            );
    }

    public async Task<JsonDocument> AnalyzeDocumentAsync(Stream documentStream)
    {
        HttpRequestMessage analyzeRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(
                $"{_endpoint}/formrecognizer/documentModels/prebuilt-receipt:analyze?api-version={_apiversion}"
            ),
            Content = new StreamContent(documentStream)
        };

        analyzeRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        analyzeRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        HttpResponseMessage analyzeResponse = await _httpClient.SendAsync(analyzeRequest);

        // Check if the response is successful
        analyzeResponse.EnsureSuccessStatusCode();

        if (!analyzeResponse.Headers.Contains("apim-request-id"))
        {
            throw new InvalidOperationException(
                "Response does not contain the header 'apim-request-id'."
            );
        }

        string requestId = analyzeResponse.Headers.GetValues("apim-request-id").FirstOrDefault() ?? string.Empty;

        //===============================================================
        string status = "running";
        JsonDocument result = null;
        while (status == "running" || status == "notStarted")
        {
            HttpRequestMessage resultRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_endpoint}/formrecognizer/documentModels/prebuilt-receipt/analyzeResults/{requestId}?api-version={_apiversion}")
            };
            resultRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            HttpResponseMessage resultResponse = await _httpClient.SendAsync(resultRequest);
            resultResponse.EnsureSuccessStatusCode();
            
            String resultJson = await resultResponse.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(resultJson);

            status = result.RootElement.GetProperty("status").GetString();

            if (status == "running")
            {
                // Wait for a specific interval before polling again
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (status == "failed")
            {
                throw new Exception("Document analysis failed.");
            }
        }

        if ((result != null )&& (status == "succeeded"))
        {
            return result;
        }
        else 
        {
            throw new Exception("Document analysis failed.");
        }
    }
}
