using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IProductBestBeforeDateService
{
    Task UpdateProductBestBeforeDatesAsync(int productId);
}

public class ProductBestBeforeDateService : IProductBestBeforeDateService
{
    private readonly ApplicationDbContext _context;
    private readonly IStockCalculationService _stockCalculation;

    public ProductBestBeforeDateService(ApplicationDbContext context, IStockCalculationService stockCalculation)
    {
        _context = context;
        _stockCalculation = stockCalculation;
    }

    public async Task UpdateProductBestBeforeDatesAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return;
        }

        // Update best before dates based on current stock
        product.BestBeforeInStock = _stockCalculation.GetEarliestBestBeforeDateInStorage(product.Batches);
        product.BestBeforeOnShelf = _stockCalculation.GetEarliestBestBeforeDateOnShelf(product.Batches);

        await _context.SaveChangesAsync();
    }
}
