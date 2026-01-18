using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Invoices API endpoints
/// </summary>
public interface IInvoicesApi
{
    [Get("/api/invoices")]
    Task<IEnumerable<InvoiceDto>> GetAllAsync();

    [Get("/api/invoices/{id}")]
    Task<InvoiceDto> GetByIdAsync(int id);

    [Post("/api/invoices")]
    Task<InvoiceDto> CreateAsync([Body] CreateInvoiceDto dto);

    [Post("/api/invoices/from-parsed")]
    Task<InvoiceDto> CreateFromParsedAsync([Body] CreateInvoiceFromParsedDto dto);

    [Post("/api/invoices/parse")]
    Task<ParseInvoiceResponse> ParseAsync([Body] ParseInvoiceRequest request);

    [Put("/api/invoices/{id}")]
    Task<InvoiceDto> UpdateAsync(int id, [Body] UpdateInvoiceDto dto);

    [Put("/api/invoices/items/{itemId}")]
    Task<InvoiceItemDto> UpdateItemAsync(int itemId, [Body] UpdateInvoiceItemDto dto);

    [Delete("/api/invoices/{id}")]
    Task DeleteAsync(int id);

    [Post("/api/invoices/manual/simple")]
    Task<InvoiceDto> CreateManualSimpleAsync([Body] CreateManualSimpleInvoiceDto dto);

    [Post("/api/invoices/manual/detailed")]
    Task<InvoiceDto> CreateManualDetailedAsync([Body] CreateManualDetailedInvoiceDto dto);
}
