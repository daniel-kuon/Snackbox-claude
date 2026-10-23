using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Tablet next to the scanner: shelf products and the session a card scan opened
/// </summary>
public interface ITabletApi
{
    [Get("/api/tablet/products")]
    Task<List<TabletProductDto>> GetProductsAsync();

    [Get("/api/tablet/state")]
    Task<TabletStateDto> GetStateAsync();

    [Post("/api/tablet/items/{productId}")]
    Task<TabletStateDto> AddItemAsync(int productId);

    [Delete("/api/tablet/items/{productId}")]
    Task<TabletStateDto> RemoveItemAsync(int productId, [Query] bool all = false);

    [Post("/api/tablet/complete")]
    Task CompleteAsync();
}
