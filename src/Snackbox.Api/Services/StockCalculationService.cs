using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IStockCalculationService
{
    int CalculateStorageQuantity(IEnumerable<ShelvingAction> shelvingActions);
    int CalculateShelfQuantity(IEnumerable<ShelvingAction> shelvingActions);
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
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
            .Sum(sa => sa.Quantity);
        var removedFromShelf = actions
            .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
            .Sum(sa => sa.Quantity);
        return addedToShelf - removedFromShelf;
    }
}
