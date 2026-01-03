using Refit;
using Snackbox.Api.DTOs;

namespace Snackbox.ApiClient;

/// <summary>
/// User Barcodes API endpoints (for login/authentication barcodes)
/// </summary>
public interface IBarcodesApi
{
    [Get("/api/barcodes")]
    Task<IEnumerable<BarcodeDto>> GetAllAsync();
    
    [Get("/api/barcodes/user/{userId}")]
    Task<IEnumerable<BarcodeDto>> GetByUserIdAsync(int userId);
    
    [Get("/api/barcodes/{id}")]
    Task<BarcodeDto> GetByIdAsync(int id);
    
    [Post("/api/barcodes")]
    Task<BarcodeDto> CreateAsync([Body] CreateBarcodeDto dto);
    
    [Put("/api/barcodes/{id}")]
    Task<BarcodeDto> UpdateAsync(int id, [Body] UpdateBarcodeDto dto);
    
    [Delete("/api/barcodes/{id}")]
    Task DeleteAsync(int id);
}
