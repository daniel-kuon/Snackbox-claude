using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface IInvoiceParserService
{
    ParseInvoiceResponse Parse(string invoiceText);
    string Format { get; }
}
