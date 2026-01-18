using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Discounts API endpoints
/// </summary>
public interface IDiscountsApi
{
    [Get("/api/discounts")]
    Task<IEnumerable<DiscountDto>> GetAllAsync([Query] bool? activeOnly = null);

    [Get("/api/discounts/{id}")]
    Task<DiscountDto> GetByIdAsync(int id);

    [Post("/api/discounts")]
    Task<DiscountDto> CreateAsync([Body] DiscountDto dto);

    [Put("/api/discounts/{id}")]
    Task UpdateAsync(int id, [Body] DiscountDto dto);

    [Delete("/api/discounts/{id}")]
    Task DeleteAsync(int id);
}
