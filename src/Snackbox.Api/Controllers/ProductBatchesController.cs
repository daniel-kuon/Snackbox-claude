using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;
using Snackbox.Api.Telemetry;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProductBatchesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductBatchesController> _logger;

    public ProductBatchesController(ApplicationDbContext context, ILogger<ProductBatchesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductBatchDto>>> GetAll()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetAllProductBatches");
        
        _logger.LogInformation("Fetching all product batches");

        var batches = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Include(pb => pb.ShelvingActions)
            .Select(pb => new ProductBatchDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                ProductName = pb.Product.Name,
                BestBeforeDate = pb.BestBeforeDate,
                QuantityInStorage = pb.ShelvingActions
                    .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
                    .Sum(sa => sa.Quantity) - pb.ShelvingActions
                    .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
                    .Sum(sa => sa.Quantity),
                QuantityOnShelf = pb.ShelvingActions
                    .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
                    .Sum(sa => sa.Quantity) - pb.ShelvingActions
                    .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                    .Sum(sa => sa.Quantity),
                CreatedAt = pb.CreatedAt
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {BatchCount} product batches", batches.Count);
        activity?.SetTag("batches.count", batches.Count);

        return Ok(batches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductBatchDto>> GetById(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetProductBatchById");
        activity?.SetTag("batch.id", id);
        
        _logger.LogInformation("Fetching product batch by ID. BatchId: {BatchId}", id);

        var batch = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("Product batch not found. BatchId: {BatchId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Batch not found");
            return NotFound(new { message = "Product batch not found" });
        }

        var dto = new ProductBatchDto
        {
            Id = batch.Id,
            ProductId = batch.ProductId,
            ProductName = batch.Product.Name,
            BestBeforeDate = batch.BestBeforeDate,
            QuantityInStorage = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
                .Sum(sa => sa.Quantity),
            QuantityOnShelf = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                .Sum(sa => sa.Quantity),
            CreatedAt = batch.CreatedAt
        };

        _logger.LogInformation(
            "Product batch retrieved. BatchId: {BatchId}, ProductId: {ProductId}, ProductName: {ProductName}, QuantityInStorage: {QuantityInStorage}, QuantityOnShelf: {QuantityOnShelf}",
            batch.Id, batch.ProductId, batch.Product.Name, dto.QuantityInStorage, dto.QuantityOnShelf);
        
        activity?.SetTag("batch.product_id", batch.ProductId);
        activity?.SetTag("batch.quantity_storage", dto.QuantityInStorage);
        activity?.SetTag("batch.quantity_shelf", dto.QuantityOnShelf);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductBatchDto>> Create([FromBody] CreateProductBatchDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CreateProductBatch");
        activity?.SetTag("batch.product_id", dto.ProductId);
        activity?.SetTag("batch.initial_quantity", dto.InitialQuantity);
        activity?.SetTag("batch.best_before_date", dto.BestBeforeDate.ToString("yyyy-MM-dd"));
        
        _logger.LogInformation(
            "Creating product batch. ProductId: {ProductId}, InitialQuantity: {InitialQuantity}, BestBeforeDate: {BestBeforeDate}",
            dto.ProductId, dto.InitialQuantity, dto.BestBeforeDate);

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            _logger.LogWarning("Batch creation failed: Product not found. ProductId: {ProductId}", dto.ProductId);
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            return BadRequest(new { message = "Product not found" });
        }

        var batch = new ProductBatch
        {
            ProductId = dto.ProductId,
            BestBeforeDate = dto.BestBeforeDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductBatches.Add(batch);
        await _context.SaveChangesAsync();

        // Add initial stock to storage
        if (dto.InitialQuantity > 0)
        {
            var shelvingAction = new ShelvingAction
            {
                ProductBatchId = batch.Id,
                Quantity = dto.InitialQuantity,
                Type = ShelvingActionType.AddedToStorage,
                ActionAt = DateTime.UtcNow
            };
            _context.ShelvingActions.Add(shelvingAction);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Product batch created successfully. BatchId: {BatchId}, ProductId: {ProductId}, ProductName: {ProductName}, InitialQuantity: {InitialQuantity}",
            batch.Id, batch.ProductId, product.Name, dto.InitialQuantity);

        SnackboxTelemetry.BatchCreatedCounter.Add(1,
            new KeyValuePair<string, object?>("product.id", dto.ProductId));
        
        activity?.SetTag("batch.id", batch.Id);

        var resultDto = new ProductBatchDto
        {
            Id = batch.Id,
            ProductId = batch.ProductId,
            ProductName = product.Name,
            BestBeforeDate = batch.BestBeforeDate,
            QuantityInStorage = dto.InitialQuantity,
            QuantityOnShelf = 0,
            CreatedAt = batch.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductBatchDto>> Update(int id, [FromBody] UpdateProductBatchDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("UpdateProductBatch");
        activity?.SetTag("batch.id", id);
        activity?.SetTag("batch.new_best_before_date", dto.BestBeforeDate.ToString("yyyy-MM-dd"));
        
        _logger.LogInformation("Updating product batch. BatchId: {BatchId}, NewBestBeforeDate: {NewBestBeforeDate}", id, dto.BestBeforeDate);

        var batch = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("Batch update failed: Batch not found. BatchId: {BatchId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Batch not found");
            return NotFound(new { message = "Product batch not found" });
        }

        var oldBestBefore = batch.BestBeforeDate;
        batch.BestBeforeDate = dto.BestBeforeDate;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product batch updated successfully. BatchId: {BatchId}, OldBestBefore: {OldBestBefore} -> NewBestBefore: {NewBestBefore}",
            batch.Id, oldBestBefore, batch.BestBeforeDate);

        var resultDto = new ProductBatchDto
        {
            Id = batch.Id,
            ProductId = batch.ProductId,
            ProductName = batch.Product.Name,
            BestBeforeDate = batch.BestBeforeDate,
            QuantityInStorage = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
                .Sum(sa => sa.Quantity),
            QuantityOnShelf = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                .Sum(sa => sa.Quantity),
            CreatedAt = batch.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpPost("{id}/move-to-shelf")]
    public async Task<ActionResult> MoveToShelf(int id, [FromBody] MoveStockDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("MoveToShelf");
        activity?.SetTag("batch.id", id);
        activity?.SetTag("stock.quantity", dto.Quantity);
        
        _logger.LogInformation("Moving stock to shelf. BatchId: {BatchId}, Quantity: {Quantity}", id, dto.Quantity);

        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("Move to shelf failed: Batch not found. BatchId: {BatchId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Batch not found");
            return NotFound(new { message = "Product batch not found" });
        }

        var storageQty = batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
            .Sum(sa => sa.Quantity) - batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
            .Sum(sa => sa.Quantity);

        if (dto.Quantity > storageQty)
        {
            _logger.LogWarning(
                "Move to shelf failed: Insufficient stock in storage. BatchId: {BatchId}, Requested: {Requested}, Available: {Available}",
                id, dto.Quantity, storageQty);
            activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
            return BadRequest(new { message = $"Not enough stock in storage. Available: {storageQty}" });
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = ShelvingActionType.MovedToShelf,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Moved stock to shelf successfully. BatchId: {BatchId}, Quantity: {Quantity}, RemainingInStorage: {RemainingInStorage}",
            batch.Id, dto.Quantity, storageQty - dto.Quantity);

        return Ok(new { message = $"Moved {dto.Quantity} items to shelf" });
    }

    [HttpPost("{id}/move-to-storage")]
    public async Task<ActionResult> MoveToStorage(int id, [FromBody] MoveStockDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("MoveToStorage");
        activity?.SetTag("batch.id", id);
        activity?.SetTag("stock.quantity", dto.Quantity);
        
        _logger.LogInformation("Moving stock to storage. BatchId: {BatchId}, Quantity: {Quantity}", id, dto.Quantity);

        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("Move to storage failed: Batch not found. BatchId: {BatchId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Batch not found");
            return NotFound(new { message = "Product batch not found" });
        }

        var shelfQty = batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
            .Sum(sa => sa.Quantity) - batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
            .Sum(sa => sa.Quantity);

        if (dto.Quantity > shelfQty)
        {
            _logger.LogWarning(
                "Move to storage failed: Insufficient stock on shelf. BatchId: {BatchId}, Requested: {Requested}, Available: {Available}",
                id, dto.Quantity, shelfQty);
            activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
            return BadRequest(new { message = $"Not enough stock on shelf. Available: {shelfQty}" });
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = ShelvingActionType.MovedFromShelf,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Moved stock to storage successfully. BatchId: {BatchId}, Quantity: {Quantity}, RemainingOnShelf: {RemainingOnShelf}",
            batch.Id, dto.Quantity, shelfQty - dto.Quantity);

        return Ok(new { message = $"Moved {dto.Quantity} items to storage" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("DeleteProductBatch");
        activity?.SetTag("batch.id", id);
        
        _logger.LogInformation("Attempting to delete product batch. BatchId: {BatchId}", id);

        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("Batch deletion failed: Batch not found. BatchId: {BatchId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Batch not found");
            return NotFound(new { message = "Product batch not found" });
        }

        var productId = batch.ProductId;
        var actionCount = batch.ShelvingActions.Count;

        _context.ProductBatches.Remove(batch);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product batch deleted successfully. BatchId: {BatchId}, ProductId: {ProductId}, ActionCount: {ActionCount}",
            id, productId, actionCount);

        return NoContent();
    }
}
