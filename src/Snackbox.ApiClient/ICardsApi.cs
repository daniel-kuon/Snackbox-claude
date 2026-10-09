using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>Numbered pre-made cards and their two purchase codes. Admin only.</summary>
public interface ICardsApi
{
    [Get("/api/cards")]
    Task<List<CardDto>> GetRangeAsync([Query] int from, [Query] int count);

    [Put("/api/cards/{number}")]
    Task<CardDto> SaveAsync(int number, [Body] SaveCardDto dto);
}
