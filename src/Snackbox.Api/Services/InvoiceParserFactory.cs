using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public class InvoiceParserFactory
{
    private readonly IEnumerable<IInvoiceParserService> _parsers;

    public InvoiceParserFactory(IEnumerable<IInvoiceParserService> parsers)
    {
        _parsers = parsers;
    }

    public IInvoiceParserService? GetParser(string format)
    {
        return _parsers.FirstOrDefault(p => 
            p.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
    }

    public IInvoiceParserService? DetectParser(string invoiceText)
    {
        // Try each parser's CanParse method to find the right one
        return _parsers.FirstOrDefault(p => p.CanParse(invoiceText));
    }

    public IEnumerable<string> GetSupportedFormats()
    {
        return _parsers.Select(p => p.Format);
    }
}
