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
    Task<TabletStateDto> CompleteAsync();

    [Post("/api/tablet/sleep")]
    Task SleepAsync();

    [Post("/api/tablet/new-purchase")]
    Task<TabletStateDto> NewPurchaseAsync();

    [Get("/api/tablet/summary")]
    Task<TabletSummaryDto> GetSummaryAsync();

    [Post("/api/tablet/pause")]
    Task PauseAsync([Query] bool paused);

    [Post("/api/tablet/achievements/claim")]
    Task<List<AchievementDto>> ClaimAchievementsAsync();

    [Get("/api/tablet/achievements")]
    Task<List<AchievementDto>> GetAchievementsAsync();

    [Post("/api/tablet/intro-seen")]
    Task IntroSeenAsync();
}
