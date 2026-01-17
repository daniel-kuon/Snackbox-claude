using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Components.Models;

namespace Snackbox.Components.Mappers;

/// <summary>
/// Maps API DTOs to UI/component models using Mapperly.
/// </summary>
[Mapper]
public static partial class DtoToModelMapper
{
    public static partial List<ScannedBarcode> ToScannedBarcodes(IEnumerable<ScannedBarcodeDto> source);
    public static partial List<RecentPurchase> ToRecentPurchases(IEnumerable<RecentPurchaseDto> source);
    public static partial List<Achievement> ToAchievements(IEnumerable<AchievementDto> source);
}
