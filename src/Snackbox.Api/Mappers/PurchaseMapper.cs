using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class PurchaseMapper
{
    /// <summary>
    /// Maps a BarcodeScan entity to a PurchaseItemDto.
    /// ProductName must be set from navigation property.
    /// </summary>
    [MapProperty(nameof(BarcodeScan.ScannedAt), nameof(PurchaseItemDto.ScannedAt))]
    [MapperIgnoreSource(nameof(BarcodeScan.PurchaseId))]
    [MapperIgnoreSource(nameof(BarcodeScan.BarcodeId))]
    [MapperIgnoreSource(nameof(BarcodeScan.Purchase))]
    [MapperIgnoreSource(nameof(BarcodeScan.Barcode))]
    [MapperIgnoreTarget(nameof(PurchaseItemDto.ProductName))]
    public static partial PurchaseItemDto ToItemDto(this BarcodeScan source);

    /// <summary>
    /// Maps a BarcodeScan entity to a PurchaseItemDto with product name.
    /// </summary>
    public static PurchaseItemDto ToItemDtoWithProductName(this BarcodeScan source)
    {
        var dto = source.ToItemDto();
        dto.ProductName = source.Barcode.Code;
        return dto;
    }

    /// <summary>
    /// Maps a list of BarcodeScan entities to a list of PurchaseItemDto.
    /// </summary>
    public static List<PurchaseItemDto> ToItemDtoList(this IEnumerable<BarcodeScan> source)
    {
        return source.Select(s => s.ToItemDtoWithProductName()).ToList();
    }

    /// <summary>
    /// Maps a Purchase entity to a PurchaseDto with calculated total amount.
    /// </summary>
    public static PurchaseDto ToDto(this Purchase source)
    {
        return new PurchaseDto
        {
            Id = source.Id,
            UserId = source.UserId,
            Username = source.User.Username,
            TotalAmount = source.ManualAmount ?? source.Scans.Sum(s => s.Amount),
            CreatedAt = source.CreatedAt,
            CompletedAt = source.CompletedAt,
            Items = source.Scans.ToItemDtoList()
        };
    }

    /// <summary>
    /// Maps a list of Purchase entities to a list of PurchaseDto.
    /// </summary>
    public static List<PurchaseDto> ToDtoList(this IEnumerable<Purchase> source)
    {
        return source.Select(p => p.ToDto()).ToList();
    }
}
