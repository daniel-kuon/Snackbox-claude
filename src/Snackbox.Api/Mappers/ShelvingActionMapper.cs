using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class ShelvingActionMapper
{
    /// <summary>
    /// Maps a ShelvingAction entity to a ShelvingActionDto.
    /// Product-related properties and BestBeforeDate must be set from navigation properties.
    /// </summary>
    [MapperIgnoreSource(nameof(ShelvingAction.ProductBatch))]
    [MapperIgnoreTarget(nameof(ShelvingActionDto.ProductId))]
    [MapperIgnoreTarget(nameof(ShelvingActionDto.ProductName))]
    [MapperIgnoreTarget(nameof(ShelvingActionDto.ProductBarcode))]
    [MapperIgnoreTarget(nameof(ShelvingActionDto.BestBeforeDate))]
    public static partial ShelvingActionDto ToDto(this ShelvingAction source);

    /// <summary>
    /// Maps a ShelvingAction entity to a ShelvingActionDto with product information.
    /// </summary>
    public static ShelvingActionDto ToDtoWithProduct(this ShelvingAction source)
    {
        var dto = source.ToDto();
        dto.ProductId = source.ProductBatch.ProductId;
        dto.BestBeforeDate = source.ProductBatch.BestBeforeDate;
        dto.ProductName = source.ProductBatch.Product.Name;
        dto.ProductBarcode = source.ProductBatch.Product.Barcodes
                                   .OrderBy(b => b.Id)
                                   .Select(b => b.Barcode)
                                   .FirstOrDefault() ?? string.Empty;
        return dto;
    }

    /// <summary>
    /// Maps a ShelvingAction entity to a ShelvingActionDto with a specified barcode.
    /// Used when the scanned barcode is known.
    /// </summary>
    public static ShelvingActionDto ToDtoWithBarcode(this ShelvingAction source, string scannedBarcode)
    {
        var dto = source.ToDtoWithProduct();
        dto.ProductBarcode = scannedBarcode;
        return dto;
    }

    /// <summary>
    /// Maps a list of ShelvingAction entities to a list of ShelvingActionDto.
    /// </summary>
    public static List<ShelvingActionDto> ToDtoList(this IEnumerable<ShelvingAction> source)
    {
        return source.Select(sa => sa.ToDtoWithProduct()).ToList();
    }

    /// <summary>
    /// Creates a ShelvingAction entity from a CreateShelvingActionDto and batch ID.
    /// </summary>
    public static ShelvingAction ToEntity(this CreateShelvingActionDto source, int productBatchId)
    {
        return new ShelvingAction
        {
            ProductBatchId = productBatchId,
            Quantity = source.Quantity,
            Type = source.Type,
            ActionAt = DateTime.UtcNow
        };
    }
}
