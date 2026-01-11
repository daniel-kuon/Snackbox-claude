using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IStockCalculationService
{
    int CalculateStorageQuantity(IEnumerable<ShelvingAction> shelvingActions);
    int CalculateShelfQuantity(IEnumerable<ShelvingAction> shelvingActions);
    double CalculateAverageProductsShelvedPerWeek(IEnumerable<ProductBatch> batches);
    DateTime? GetEarliestBestBeforeDateInStorage(IEnumerable<ProductBatch> batches);
    DateTime? GetEarliestBestBeforeDateOnShelf(IEnumerable<ProductBatch> batches);
}

public class StockCalculationService : IStockCalculationService
{
    public int CalculateStorageQuantity(IEnumerable<ShelvingAction> shelvingActions)
    {
        var actions = shelvingActions.ToList();
        var addedToStorage = actions
            .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
            .Sum(sa => sa.Quantity);
        var removedFromStorage = actions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
            .Sum(sa => sa.Quantity);
        return addedToStorage - removedFromStorage;
    }

    public int CalculateShelfQuantity(IEnumerable<ShelvingAction> shelvingActions)
    {
        var actions = shelvingActions.ToList();
        var addedToShelf = actions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.AddedToShelf)
            .Sum(sa => sa.Quantity);
        var removedFromShelf = actions
            .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf || sa.Type == ShelvingActionType.Consumed)
            .Sum(sa => sa.Quantity);
        return addedToShelf - removedFromShelf;
    }

    public double CalculateAverageProductsShelvedPerWeek(IEnumerable<ProductBatch> batches)
    {
        var allActions = batches.SelectMany(b => b.ShelvingActions).ToList();
        
        // Get actions that add items to shelf (MovedToShelf, AddedToShelf)
        var shelfAdditions = allActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.AddedToShelf)
            .OrderBy(sa => sa.ActionAt)
            .ToList();
        
        if (!shelfAdditions.Any())
        {
            return 0;
        }
        
        // Calculate time span from first to last shelving action
        var firstAction = shelfAdditions.First().ActionAt;
        var lastAction = shelfAdditions.Last().ActionAt;
        var timeSpan = lastAction - firstAction;
        
        // If all actions happened on the same day, consider it as 1 week minimum
        var weeks = timeSpan.TotalDays < 7 ? 1 : timeSpan.TotalDays / 7.0;
        
        // Sum total quantity added to shelf
        var totalQuantity = shelfAdditions.Sum(sa => sa.Quantity);
        
        return totalQuantity / weeks;
    }

    public DateTime? GetEarliestBestBeforeDateInStorage(IEnumerable<ProductBatch> batches)
    {
        var batchList = batches.ToList();
        
        // Pre-calculate stock quantities to avoid O(n²) complexity
        var batchesWithStockQuantities = batchList
            .Select(b => new { Batch = b, Quantity = CalculateStorageQuantity(b.ShelvingActions) })
            .Where(x => x.Quantity > 0)
            .ToList();
        
        return batchesWithStockQuantities.Any() 
            ? batchesWithStockQuantities.Min(x => x.Batch.BestBeforeDate) 
            : null;
    }

    public DateTime? GetEarliestBestBeforeDateOnShelf(IEnumerable<ProductBatch> batches)
    {
        var batchList = batches.ToList();
        
        // Pre-calculate stock quantities to avoid O(n²) complexity
        var batchesWithStockQuantities = batchList
            .Select(b => new { Batch = b, Quantity = CalculateShelfQuantity(b.ShelvingActions) })
            .Where(x => x.Quantity > 0)
            .ToList();
        
        return batchesWithStockQuantities.Any() 
            ? batchesWithStockQuantities.Min(x => x.Batch.BestBeforeDate) 
            : null;
    }
}
