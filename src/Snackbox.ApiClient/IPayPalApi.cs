using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>Received PayPal payments with "Snackbox" in the note, and who paid them. Admin only.</summary>
public interface IPayPalApi
{
    [Get("/api/paypal/status")]
    Task<PayPalStatusDto> GetStatusAsync();

    [Post("/api/paypal/sync")]
    Task<PayPalStatusDto> SyncAsync();

    [Get("/api/paypal/transactions")]
    Task<List<PayPalTransactionDto>> GetTransactionsAsync([Query] int count = 200);

    [Post("/api/paypal/transactions/{id}/assign")]
    Task<PayPalStatusDto> AssignAsync(int id, [Body] AssignPayPalPayerDto dto);

    [Post("/api/paypal/transactions/{id}/dismiss")]
    Task DismissAsync(int id, [Query] bool dismissed = true);
}
