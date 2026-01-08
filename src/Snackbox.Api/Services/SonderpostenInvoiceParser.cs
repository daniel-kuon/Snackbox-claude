using System.Globalization;
using System.Text.RegularExpressions;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public class SonderpostenInvoiceParser : IInvoiceParserService
{
    public string Format => "sonderposten";

    public ParseInvoiceResponse Parse(string invoiceText)
    {
        var response = new ParseInvoiceResponse { Success = true };

        try
        {
            // Extract metadata
            response.Metadata = ExtractMetadata(invoiceText);

            // Parse invoice items
            response.Items = ParseItems(invoiceText);

            if (!response.Items.Any())
            {
                response.Success = false;
                response.ErrorMessage = "No items found in invoice";
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Failed to parse invoice: {ex.Message}";
        }

        return response;
    }

    private InvoiceMetadata ExtractMetadata(string invoiceText)
    {
        var metadata = new InvoiceMetadata();

        // Extract invoice number (Belegnummer)
        var invoiceNumberMatch = Regex.Match(invoiceText, @"Belegnummer\s+(\d+)");
        if (invoiceNumberMatch.Success)
        {
            metadata.InvoiceNumber = invoiceNumberMatch.Groups[1].Value;
        }

        // Extract date (Datum: 21.07.2025, 12:45:24)
        var dateMatch = Regex.Match(invoiceText, @"Datum:\s+(\d{2}\.\d{2}\.\d{4})");
        if (dateMatch.Success)
        {
            if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                metadata.InvoiceDate = date;
            }
        }

        // Supplier is Lebensmittel-Sonderposten (Hapex GmbH)
        metadata.Supplier = "Lebensmittel-Sonderposten";

        return metadata;
    }

    private List<ParsedInvoiceItem> ParseItems(string invoiceText)
    {
        var items = new List<ParsedInvoiceItem>();

        // Pattern to match lines like:
        // 1 SW25617 M&Ms USA Peanut Butter Chocolate Candies 963,9g MHD:30.7.25 2 7 % 21,00 € 42,00 €
        // Also match lines without article number for shipping:
        // 24 Versand + Verpackungskosten 1 7 % 6,99 € 6,99 €
        
        var itemPattern = @"^\s*(\d+)\s+(SW\d+)?\s*(.+?)\s+(\d+)\s+\d+\s*%\s+([\d,]+)\s*€\s+([\d,]+)\s*€";
        var lines = invoiceText.Split('\n');

        foreach (var line in lines)
        {
            var match = Regex.Match(line, itemPattern);
            if (match.Success)
            {
                var productName = match.Groups[3].Value.Trim();
                var articleNumber = match.Groups[2].Success ? match.Groups[2].Value : null;
                
                // Skip shipping/packaging costs (they don't have article numbers)
                if (string.IsNullOrEmpty(articleNumber) || 
                    productName.Contains("Versand") || 
                    productName.Contains("Verpackungskosten"))
                {
                    continue;
                }
                
                var quantity = int.Parse(match.Groups[4].Value);
                var unitPriceStr = match.Groups[5].Value.Replace(",", ".");
                var totalPriceStr = match.Groups[6].Value.Replace(",", ".");

                if (decimal.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) &&
                    decimal.TryParse(totalPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalPrice))
                {
                    // Extract best before date (MHD:30.7.25) and remove it from product name
                    DateTime? bestBefore = null;
                    var mhdMatch = Regex.Match(productName, @"MHD:(\d{1,2})\.(\d{1,2})\.(\d{2,4})");
                    if (mhdMatch.Success)
                    {
                        var day = int.Parse(mhdMatch.Groups[1].Value);
                        var month = int.Parse(mhdMatch.Groups[2].Value);
                        var year = int.Parse(mhdMatch.Groups[3].Value);
                        
                        // Convert 2-digit year to 4-digit (25 -> 2025)
                        if (year < 100)
                        {
                            year += 2000;
                        }

                        try
                        {
                            bestBefore = new DateTime(year, month, day);
                        }
                        catch
                        {
                            // Invalid date, ignore
                        }
                        
                        // Remove MHD date from product name
                        productName = Regex.Replace(productName, @"\s*MHD:\d{1,2}\.\d{1,2}\.\d{2,4}\s*", " ").Trim();
                    }

                    items.Add(new ParsedInvoiceItem
                    {
                        ProductName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice,
                        BestBeforeDate = bestBefore,
                        ArticleNumber = articleNumber
                    });
                }
            }
        }

        return items;
    }
}
