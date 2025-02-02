using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

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
            JsonDocument result = await _invoiceRecognizerService.AnalyzeDocumentAsync(stream);
            return Ok(result);
        }
    }
}