namespace InvoiceRecognizer.DTO;

public class ExtractReceiptDTO
{
    public string MerchantName { get; set; }
    public double Total { get; set; }
    public double TotalTax { get; set; }
    public DateTime TransactionDate { get; set; }
}
