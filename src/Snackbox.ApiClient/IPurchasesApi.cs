using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Purchases API endpoints
/// </summary>
public interface IPurchasesApi
{
    [Get("/api/purchases/my-purchases")]
    Task<IEnumerable<PurchaseDto>> GetMyPurchasesAsync();
    
    [Get("/api/purchases")]
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    
    [Get("/api/purchases/user/{userId}")]
    Task<IEnumerable<PurchaseDto>> GetByUserIdAsync(int userId);
    
    [Get("/api/purchases/{id}")]
    Task<PurchaseDto> GetByIdAsync(int id);
}
