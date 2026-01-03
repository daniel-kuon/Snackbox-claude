using Refit;
using Snackbox.Api.DTOs;

namespace Snackbox.ApiClient;

/// <summary>
/// Shelving Actions API endpoints
/// </summary>
public interface IShelvingActionsApi
{
    [Get("/api/shelvingactions")]
    Task<IEnumerable<ShelvingActionDto>> GetAllAsync([Query] int? productId = null, [Query] int? limit = null);
    
    [Get("/api/shelvingactions/{id}")]
    Task<ShelvingActionDto> GetByIdAsync(int id);
    
    [Post("/api/shelvingactions")]
    Task<ShelvingActionDto> CreateAsync([Body] CreateShelvingActionDto dto);
    
    [Post("/api/shelvingactions/batch")]
    Task<BatchShelvingResponse> CreateBatchAsync([Body] BatchShelvingRequest request);
}
