using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class ProductMapper
{
    /// <summary>
    /// Maps a ProductBarcode entity to a ProductBarcodeDto.
    /// </summary>
    [MapperIgnoreSource(nameof(ProductBarcode.Product))]
    public static partial ProductBarcodeDto ToDto(this ProductBarcode source);

    /// <summary>
    /// Maps a list of ProductBarcode entities to a list of ProductBarcodeDto.
    /// </summary>
    public static partial List<ProductBarcodeDto> ToDtoList(this IEnumerable<ProductBarcode> source);

    /// <summary>
    /// Maps a Product entity to a ProductDto.
    /// Extension method is implemented manually to handle nested Barcodes mapping.
    /// </summary>
    public static ProductDto ToDto(this Product source, IStockCalculationService? stockCalculation = null)
    {
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            CreatedAt = source.CreatedAt,
            BestBeforeInStock = source.BestBeforeInStock,
            BestBeforeOnShelf = source.BestBeforeOnShelf,
            AverageProductsShelvedPerWeek = stockCalculation != null 
                ? stockCalculation.CalculateAverageProductsShelvedPerWeek(source.Batches)
                : 0,
            Barcodes = source.Barcodes.ToDtoList()
        };
    }

    /// <summary>
    /// Maps a list of Product entities to a list of ProductDto.
    /// </summary>
    public static List<ProductDto> ToDtoList(this IEnumerable<Product> source, IStockCalculationService? stockCalculation = null)
    {
        return source.Select(p => p.ToDto(stockCalculation)).ToList();
    }
}
