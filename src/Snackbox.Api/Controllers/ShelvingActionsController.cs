using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ShelvingActionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShelvingActionsController> _logger;

    public ShelvingActionsController(ApplicationDbContext context, ILogger<ShelvingActionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShelvingActionDto>>> GetAll([FromQuery] int? productId = null, [FromQuery] int? limit = null)
    {
        var query = _context.ShelvingActions
            .Include(sa => sa.ProductBatch)
            .ThenInclude(pb => pb.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(sa => sa.ProductBatch.ProductId == productId.Value);
        }

        query = query.OrderByDescending(sa => sa.ActionAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var actions = await query
            .Select(sa => new ShelvingActionDto
            {
                Id = sa.Id,
                ProductBatchId = sa.ProductBatchId,
                ProductId = sa.ProductBatch.ProductId,
                ProductName = sa.ProductBatch.Product.Name,
                ProductBarcode = sa.ProductBatch.Product.Barcode,
                BestBeforeDate = sa.ProductBatch.BestBeforeDate,
                Quantity = sa.Quantity,
                Type = sa.Type.ToString(),
                ActionAt = sa.ActionAt
            })
            .ToListAsync();

        return Ok(actions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShelvingActionDto>> GetById(int id)
    {
        var action = await _context.ShelvingActions
            .Include(sa => sa.ProductBatch)
            .ThenInclude(pb => pb.Product)
            .FirstOrDefaultAsync(sa => sa.Id == id);

        if (action == null)
        {
            return NotFound(new { message = "Shelving action not found" });
        }

        var dto = new ShelvingActionDto
        {
            Id = action.Id,
            ProductBatchId = action.ProductBatchId,
            ProductId = action.ProductBatch.ProductId,
            ProductName = action.ProductBatch.Product.Name,
            ProductBarcode = action.ProductBatch.Product.Barcode,
            BestBeforeDate = action.ProductBatch.BestBeforeDate,
            Quantity = action.Quantity,
            Type = action.Type.ToString(),
            ActionAt = action.ActionAt
        };

        return Ok(dto);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<ShelvingActionDto>>> CreateBatch([FromBody] BatchShelvingRequest request)
    {
        var results = new List<ShelvingActionDto>();
        var errors = new List<string>();

        foreach (var action in request.Actions)
        {
            try
            {
                // Find product by barcode
                var product = await _context.Products
                    .Include(p => p.Batches)
                    .ThenInclude(b => b.ShelvingActions)
                    .FirstOrDefaultAsync(p => p.Barcode == action.ProductBarcode);

                if (product == null)
                {
                    errors.Add($"Product with barcode '{action.ProductBarcode}' not found");
                    continue;
                }

                // Parse the action type
                if (!Enum.TryParse<ShelvingActionType>(action.Type, out var actionType))
                {
                    errors.Add($"Invalid action type '{action.Type}' for product '{action.ProductBarcode}'");
                    continue;
                }

                // Find or create batch for the given best-before date
                var batch = product.Batches.FirstOrDefault(b => b.BestBeforeDate.Date == action.BestBeforeDate.Date);
                
                if (batch == null)
                {
                    // Create new batch if not exists
                    batch = new ProductBatch
                    {
                        ProductId = product.Id,
                        BestBeforeDate = action.BestBeforeDate.Date,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductBatches.Add(batch);
                    await _context.SaveChangesAsync();
                }

                // Validate stock availability for certain action types
                if (actionType == ShelvingActionType.MovedToShelf)
                {
                    var storageQty = batch.ShelvingActions
                        .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
                        .Sum(sa => sa.Quantity) - batch.ShelvingActions
                        .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
                        .Sum(sa => sa.Quantity);

                    if (action.Quantity > storageQty)
                    {
                        errors.Add($"Not enough stock in storage for product '{action.ProductBarcode}'. Available: {storageQty}");
                        continue;
                    }
                }
                else if (actionType == ShelvingActionType.MovedFromShelf || actionType == ShelvingActionType.RemovedFromShelf)
                {
                    var shelfQty = batch.ShelvingActions
                        .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
                        .Sum(sa => sa.Quantity) - batch.ShelvingActions
                        .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                        .Sum(sa => sa.Quantity);

                    if (action.Quantity > shelfQty)
                    {
                        errors.Add($"Not enough stock on shelf for product '{action.ProductBarcode}'. Available: {shelfQty}");
                        continue;
                    }
                }

                // Create the shelving action
                var shelvingAction = new ShelvingAction
                {
                    ProductBatchId = batch.Id,
                    Quantity = action.Quantity,
                    Type = actionType,
                    ActionAt = DateTime.UtcNow
                };

                _context.ShelvingActions.Add(shelvingAction);
                await _context.SaveChangesAsync();

                results.Add(new ShelvingActionDto
                {
                    Id = shelvingAction.Id,
                    ProductBatchId = batch.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductBarcode = product.Barcode,
                    BestBeforeDate = batch.BestBeforeDate,
                    Quantity = shelvingAction.Quantity,
                    Type = shelvingAction.Type.ToString(),
                    ActionAt = shelvingAction.ActionAt
                });

                _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
                    actionType, action.Quantity, product.Name, batch.Id);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing product '{action.ProductBarcode}': {ex.Message}");
            }
        }

        if (errors.Any() && !results.Any())
        {
            return BadRequest(new { message = "All actions failed", errors });
        }

        if (errors.Any())
        {
            return Ok(new { results, errors, message = "Some actions completed with errors" });
        }

        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult<ShelvingActionDto>> Create([FromBody] CreateShelvingActionDto dto)
    {
        // Find product by barcode
        var product = await _context.Products
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(p => p.Barcode == dto.ProductBarcode);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        // Parse the action type
        if (!Enum.TryParse<ShelvingActionType>(dto.Type, out var actionType))
        {
            return BadRequest(new { message = "Invalid action type" });
        }

        // Find or create batch for the given best-before date
        var batch = product.Batches.FirstOrDefault(b => b.BestBeforeDate.Date == dto.BestBeforeDate.Date);

        if (batch == null)
        {
            batch = new ProductBatch
            {
                ProductId = product.Id,
                BestBeforeDate = dto.BestBeforeDate.Date,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductBatches.Add(batch);
            await _context.SaveChangesAsync();
        }

        // Validate stock availability
        if (actionType == ShelvingActionType.MovedToShelf)
        {
            var storageQty = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
                .Sum(sa => sa.Quantity);

            if (dto.Quantity > storageQty)
            {
                return BadRequest(new { message = $"Not enough stock in storage. Available: {storageQty}" });
            }
        }
        else if (actionType == ShelvingActionType.MovedFromShelf || actionType == ShelvingActionType.RemovedFromShelf)
        {
            var shelfQty = batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
                .Sum(sa => sa.Quantity) - batch.ShelvingActions
                .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                .Sum(sa => sa.Quantity);

            if (dto.Quantity > shelfQty)
            {
                return BadRequest(new { message = $"Not enough stock on shelf. Available: {shelfQty}" });
            }
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = actionType,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
            actionType, dto.Quantity, product.Name, batch.Id);

        var result = new ShelvingActionDto
        {
            Id = shelvingAction.Id,
            ProductBatchId = batch.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            ProductBarcode = product.Barcode,
            BestBeforeDate = batch.BestBeforeDate,
            Quantity = shelvingAction.Quantity,
            Type = shelvingAction.Type.ToString(),
            ActionAt = shelvingAction.ActionAt
        };

        return CreatedAtAction(nameof(GetById), new { id = shelvingAction.Id }, result);
    }
}
