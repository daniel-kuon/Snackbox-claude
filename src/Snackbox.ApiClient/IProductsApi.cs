using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Products API endpoints
/// </summary>
public interface IProductsApi
{
    [Get("/api/products")]
    Task<IEnumerable<ProductDto>> GetAllAsync();
    
    [Get("/api/products/{id}")]
    Task<ProductDto> GetByIdAsync(int id);
    
    [Post("/api/products")]
    Task<ProductDto> CreateAsync([Body] CreateProductDto dto);
    
    [Put("/api/products/{id}")]
    Task<ProductDto> UpdateAsync(int id, [Body] UpdateProductDto dto);
    
    [Delete("/api/products/{id}")]
    Task DeleteAsync(int id);
    
    [Get("/api/products/barcode/{barcode}")]
    Task<ProductDto> GetByBarcodeAsync(string barcode);
    
    [Get("/api/products/{id}/stock")]
    Task<ProductStockDto> GetProductStockAsync(int id);
    
    [Get("/api/products/stock")]
    Task<IEnumerable<ProductStockDto>> GetAllProductStockAsync();
    
    // Barcode management endpoints
    [Post("/api/products/{id}/barcodes")]
    Task<ProductBarcodeDto> AddBarcodeAsync(int id, [Body] CreateProductBarcodeDto dto);
    
    [Put("/api/products/{productId}/barcodes/{barcodeId}")]
    Task<ProductBarcodeDto> UpdateBarcodeAsync(int productId, int barcodeId, [Body] UpdateProductBarcodeDto dto);
    
    [Delete("/api/products/{productId}/barcodes/{barcodeId}")]
    Task DeleteBarcodeAsync(int productId, int barcodeId);
}
