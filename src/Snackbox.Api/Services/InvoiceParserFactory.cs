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

    public IEnumerable<string> GetSupportedFormats()
    {
        return _parsers.Select(p => p.Format);
    }
}
