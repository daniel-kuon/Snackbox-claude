using System.Globalization;
using System.Text.RegularExpressions;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public partial class ReweInvoiceParser : IInvoiceParserService
{
    public string Format => "rewe";

    [GeneratedRegex(@"Datum:\s*(\d{2}\.\d{2}\.\d{4})", RegexOptions.Multiline)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"Beleg-Nr\.\s*(\d+)", RegexOptions.Multiline)]
    private static partial Regex InvoiceNumberRegex();

    [GeneratedRegex(@"SUMME\s+EUR\s+([\d,]+)", RegexOptions.Multiline)]
    private static partial Regex TotalRegex();

    [GeneratedRegex(@"^([A-ZÄÖÜ\s\.]+(?:\d+)?)[\s]+([\d,]+)\s+B\s*$", RegexOptions.Multiline)]
    private static partial Regex SimpleItemRegex();

    [GeneratedRegex(@"^(\d+)\s+Stk\s+x\s+([\d,]+)\s*$", RegexOptions.Multiline)]
    private static partial Regex QuantityLineRegex();

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

        // Extract date (Datum: 29.12.2025)
        var dateMatch = DateRegex().Match(invoiceText);
        if (dateMatch.Success)
        {
            if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                metadata.InvoiceDate = date;
            }
        }

        // Extract invoice number (Beleg-Nr. 9862)
        var invoiceNumberMatch = InvoiceNumberRegex().Match(invoiceText);
        if (invoiceNumberMatch.Success)
        {
            metadata.InvoiceNumber = invoiceNumberMatch.Groups[1].Value;
        }

        // Supplier is REWE
        metadata.Supplier = "REWE";

        // Extract total amount (SUMME EUR 34,21)
        var totalMatch = TotalRegex().Match(invoiceText);
        if (totalMatch.Success)
        {
            var totalStr = totalMatch.Groups[1].Value.Replace(",", ".");
            if (decimal.TryParse(totalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
            {
                metadata.TotalAmount = total;
            }
        }

        return metadata;
    }

    private List<ParsedInvoiceItem> ParseItems(string invoiceText)
    {
        var items = new List<ParsedInvoiceItem>();
        var lines = invoiceText.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Skip empty lines, headers, and non-product lines
            if (string.IsNullOrWhiteSpace(line) || 
                line.Contains("-----") ||
                line.Contains("SUMME") ||
                line.Contains("Geg.") ||
                line.Contains("Mastercard") ||
                line.Contains("REWE") ||
                line.Contains("Steuer") ||
                line.Contains("TSE") ||
                line.Contains("Bonus") ||
                line.Contains("EUR") && !line.Contains(" B"))
            {
                continue;
            }

            // Pattern matches lines like: "POM.LEBERW.FEIN 1,19 B"
            var match = SimpleItemRegex().Match(line);
            if (match.Success)
            {
                var productName = match.Groups[1].Value.Trim();
                var priceStr = match.Groups[2].Value.Replace(",", ".");
                
                if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    var quantity = 1;
                    var unitPrice = price;
                    var totalPrice = price;

                    // Check if the next line contains quantity info (e.g., "3 Stk x 1,75")
                    if (i + 1 < lines.Length)
                    {
                        var nextLine = lines[i + 1].Trim();
                        var qtyMatch = QuantityLineRegex().Match(nextLine);
                        if (qtyMatch.Success)
                        {
                            quantity = int.Parse(qtyMatch.Groups[1].Value);
                            var unitPriceStr = qtyMatch.Groups[2].Value.Replace(",", ".");
                            if (decimal.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice))
                            {
                                totalPrice = price; // The first line already has the total
                            }
                            i++; // Skip the quantity line since we've processed it
                        }
                    }

                    items.Add(new ParsedInvoiceItem
                    {
                        ProductName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    });
                }
            }
        }

        return items;
    }
}
