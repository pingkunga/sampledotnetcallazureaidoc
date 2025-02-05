using System.Text.Json;
using InvoiceRecognizer.DTO;
using InvoiceRecognizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceRecognizer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceRecognizerController : ControllerBase
{
    private readonly InvoiceRecognizerService _invoiceRecognizerService;

    public InvoiceRecognizerController(InvoiceRecognizerService pInvoiceRecognizerService)
    {
        _invoiceRecognizerService = pInvoiceRecognizerService;
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
}