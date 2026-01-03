using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Payments API endpoints
/// </summary>
public interface IPaymentsApi
{
    [Get("/api/payments")]
    Task<IEnumerable<PaymentDto>> GetAllAsync();
    
    [Get("/api/payments/my-payments")]
    Task<IEnumerable<PaymentDto>> GetMyPaymentsAsync();
    
    [Get("/api/payments/user/{userId}")]
    Task<IEnumerable<PaymentDto>> GetByUserIdAsync(int userId);
    
    [Post("/api/payments")]
    Task<PaymentDto> CreateAsync([Body] CreatePaymentDto dto);
}
