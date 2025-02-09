using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using InvoiceRecognizer.DTO;
using InvoiceRecognizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceRecognizer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceRecognizerController : ControllerBase
{
    private readonly InvoiceRecognizerService _invoiceRecognizerService;
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;

    public InvoiceRecognizerController(InvoiceRecognizerService pInvoiceRecognizerService, DocumentIntelligenceClient pDocumentIntelligenceClient)
    {
        _invoiceRecognizerService = pInvoiceRecognizerService;
        _documentIntelligenceClient = pDocumentIntelligenceClient;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using (var stream = file.OpenReadStream())
        {
            try
            {
                JsonDocument result = await _invoiceRecognizerService.AnalyzeDocumentAsync(stream);
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Request error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(408, "The request timed out.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }

    [HttpPost("extractreceipt")]
    public async Task<IActionResult> ExtractReceipt(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using (var stream = file.OpenReadStream())
        {
            try
            {
                ExtractReceiptDTO result = await _invoiceRecognizerService.ExtractReceipt(stream);
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Request error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(408, "The request timed out.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }

    [HttpPost("analyzesdk")]
    public async Task<IActionResult> AnalyzeDocumentWithSDK(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            
            var binaryData = BinaryData.FromStream(stream);
            var modelId = "prebuilt-receipt"; 
            
            try
            {
                Operation<AnalyzeResult>  operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, binaryData);
                AnalyzeResult result = operation.Value;
                return Ok(result);
            }
            catch (RequestFailedException ex)
            {
                return StatusCode((int)ex.Status, ex.Message);
            }
        }
    }

    [HttpPost("extractreceiptsdk")]
    public async Task<IActionResult> ExtractReceiptWithSDK(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            
            var binaryData = BinaryData.FromStream(stream);
            var modelId = "prebuilt-receipt"; 
            
            try
            {
                Operation<AnalyzeResult>  operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, binaryData);
                AnalyzeResult result = operation.Value;

                if (result.Documents.Count == 0)
                {
                    return BadRequest("No document was recognized.");
                }
                //result.Documents can contain multiple documents, but we only get first one in this example
                AnalyzedDocument document = result.Documents[0];

                string _MerchantName = "N/A";
                if (document.Fields.TryGetValue("MerchantName", out DocumentField MerchantNameField)
                    && MerchantNameField.FieldType == DocumentFieldType.String)
                {
                    _MerchantName = MerchantNameField.ValueString;
                }

                double? _Total = null;
                if (document.Fields.TryGetValue("Total", out DocumentField TotalField)
                    && TotalField.FieldType == DocumentFieldType.Currency)
                {
                    _Total = TotalField.ValueCurrency.Amount;
                }

                double? _TotalTax = null;
                if (document.Fields.TryGetValue("TotalTotalTax", out DocumentField TotalTaxField)
                    && TotalTaxField.FieldType == DocumentFieldType.Currency)
                {
                    _TotalTax = TotalTaxField.ValueCurrency.Amount;
                }

                DateTimeOffset? _TransactionDate = null;
                if (document.Fields.TryGetValue("TransactionDate", out DocumentField TransactionDateField)
                    && TransactionDateField.FieldType == DocumentFieldType.Date)
                {
                    _TransactionDate = TransactionDateField.ValueDate;
                }

                ExtractReceiptDTO extractReceiptDTO = new ExtractReceiptDTO
                {
                    MerchantName = _MerchantName,
                    Total = _Total.GetValueOrDefault(0),
                    TotalTax = _TotalTax.GetValueOrDefault(0),
                    TransactionDate = _TransactionDate.GetValueOrDefault(DateTimeOffset.MinValue).DateTime
                };
                return Ok(extractReceiptDTO);
            }
            catch (RequestFailedException ex)
            {
                return StatusCode((int)ex.Status, ex.Message);
            }
        }
    }

    [HttpPost("analyzesdk2")]
    public async Task<IActionResult> AnalyzeDocumentWithSDK2(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            using RequestContent content = RequestContent.Create(new
            {
                base64Source = await ConvertIFormFileToBase64(file)
            });
            Operation<BinaryData> operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", content);
            BinaryData responseData = operation.Value;

            JsonElement result = JsonDocument.Parse(responseData.ToStream()).RootElement;
            return Ok(result);
        }
        catch (RequestFailedException ex)
        {
            return StatusCode((int)ex.Status, ex.Message);
        }
    }

    public async Task<string> ConvertIFormFileToBase64(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            return Convert.ToBase64String(fileBytes);
        }
    }
}