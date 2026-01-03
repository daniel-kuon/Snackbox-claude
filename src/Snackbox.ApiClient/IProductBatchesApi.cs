using Refit;
using Snackbox.Api.DTOs;

namespace Snackbox.ApiClient;

/// <summary>
/// Product Batches API endpoints
/// </summary>
public interface IProductBatchesApi
{
    [Get("/api/productbatches")]
    Task<IEnumerable<ProductBatchDto>> GetAllAsync();
    
    [Get("/api/productbatches/{id}")]
    Task<ProductBatchDto> GetByIdAsync(int id);
    
    [Get("/api/productbatches/product/{productId}")]
    Task<IEnumerable<ProductBatchDto>> GetByProductIdAsync(int productId);
    
    [Post("/api/productbatches")]
    Task<ProductBatchDto> CreateAsync([Body] CreateProductBatchDto dto);
    
    [Put("/api/productbatches/{id}")]
    Task<ProductBatchDto> UpdateAsync(int id, [Body] UpdateProductBatchDto dto);
    
    [Delete("/api/productbatches/{id}")]
    Task DeleteAsync(int id);
    
    [Post("/api/productbatches/{id}/move-to-shelf")]
    Task<ProductBatchDto> MoveToShelfAsync(int id, [Body] MoveStockDto dto);
    
    [Post("/api/productbatches/{id}/move-from-shelf")]
    Task<ProductBatchDto> MoveFromShelfAsync(int id, [Body] MoveStockDto dto);
}
