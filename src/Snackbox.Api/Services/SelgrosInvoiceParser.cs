using System.Globalization;
using System.Text.RegularExpressions;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public class SelgrosInvoiceParser : IInvoiceParserService
{
    public string Format => "selgros";

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
        var invoiceNumberMatch = Regex.Match(invoiceText, @"Belegnummer:\s*(\d+)");
        if (invoiceNumberMatch.Success)
        {
            metadata.InvoiceNumber = invoiceNumberMatch.Groups[1].Value;
        }

        // Extract date (Belegdatum: 20.12.2025 18:00)
        var dateMatch = Regex.Match(invoiceText, @"Belegdatum:\s*(\d{2}\.\d{2}\.\d{4})");
        if (dateMatch.Success)
        {
            if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                metadata.InvoiceDate = date;
            }
        }

        // Supplier is Selgros
        metadata.Supplier = "Selgros";

        // Extract total amount (EUR 370,33)
        var totalMatch = Regex.Match(invoiceText, @"EUR\s+([\d,]+)\s*$", RegexOptions.Multiline);
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

        // Pattern to match lines like:
        // 1 4059586509519 SCHWEINEGESCHNETZELTES GYROS 1,145 1 kg 8,400 9,62 7,0 %
        // Pos. GTIN Bezeichnung Menge Inhalt VP Einzelpreis* Warenwert* MwSt
        
        var itemPattern = @"^\s*(\d+)\s+(\d{13})\s+(.+?)\s+([\d,]+)\s+[\d\s]+(?:kg|PG|ST|EI|BT|BE|DS|FL|GL|TB)\s+(?:[MA]\s+)?([\d,]+)\s+([\d,]+)\s+[\d,]+\s*%";
        var lines = invoiceText.Split('\n');

        foreach (var line in lines)
        {
            var match = Regex.Match(line, itemPattern);
            if (match.Success)
            {
                var gtin = match.Groups[2].Value;
                var productName = match.Groups[3].Value.Trim();
                var quantityStr = match.Groups[4].Value.Replace(",", ".");
                var unitPriceStr = match.Groups[5].Value.Replace(",", ".");
                var totalPriceStr = match.Groups[6].Value.Replace(",", ".");

                if (decimal.TryParse(quantityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantityDec) &&
                    decimal.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) &&
                    decimal.TryParse(totalPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalPrice))
                {
                    // Round quantity to nearest integer for most products
                    var quantity = (int)Math.Round(quantityDec);
                    if (quantity == 0) quantity = 1;

                    // Skip deposit/Pfand items
                    if (productName.Contains("DPG") || productName.Contains("FLASCHE") || productName.Contains("DOSE"))
                    {
                        // Check if this is ONLY a deposit line (not a product with deposit)
                        if (match.Groups[2].Value == "879170")
                        {
                            continue;
                        }
                    }

                    items.Add(new ParsedInvoiceItem
                    {
                        ProductName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice,
                        ArticleNumber = gtin
                    });
                }
            }
        }

        return items;
    }
}
