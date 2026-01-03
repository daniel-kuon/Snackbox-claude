using Riok.Mapperly.Abstractions;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class ProductBatchMapper
{
    /// <summary>
    /// Maps a ProductBatch entity to a ProductBatchDto.
    /// ProductName and stock quantities must be set separately.
    /// </summary>
    [MapperIgnoreSource(nameof(ProductBatch.Product))]
    [MapperIgnoreSource(nameof(ProductBatch.ShelvingActions))]
    [MapperIgnoreTarget(nameof(ProductBatchDto.ProductName))]
    [MapperIgnoreTarget(nameof(ProductBatchDto.QuantityInStorage))]
    [MapperIgnoreTarget(nameof(ProductBatchDto.QuantityOnShelf))]
    public static partial ProductBatchDto ToDto(this ProductBatch source);

    /// <summary>
    /// Maps a ProductBatch entity to a ProductBatchDto with product name and stock quantities.
    /// </summary>
    /// <param name="source">The ProductBatch entity.</param>
    /// <param name="quantityInStorage">Calculated quantity in storage.</param>
    /// <param name="quantityOnShelf">Calculated quantity on shelf.</param>
    public static ProductBatchDto ToDtoWithStock(this ProductBatch source, int quantityInStorage, int quantityOnShelf)
    {
        var dto = source.ToDto();
        dto.ProductName = source.Product?.Name;
        dto.QuantityInStorage = quantityInStorage;
        dto.QuantityOnShelf = quantityOnShelf;
        return dto;
    }

    /// <summary>
    /// Creates a ProductBatch entity from a CreateProductBatchDto.
    /// </summary>
    public static ProductBatch ToEntity(this CreateProductBatchDto source)
    {
        return new ProductBatch
        {
            ProductId = source.ProductId,
            BestBeforeDate = source.BestBeforeDate,
            CreatedAt = DateTime.UtcNow
        };
    }
}
