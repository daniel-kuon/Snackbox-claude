using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ShelvingActionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShelvingActionsController> _logger;
    private readonly IStockCalculationService _stockCalculation;

    public ShelvingActionsController(ApplicationDbContext context, ILogger<ShelvingActionsController> logger, IStockCalculationService stockCalculation)
    {
        _context = context;
        _logger = logger;
        _stockCalculation = stockCalculation;
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
                ProductBarcode = sa.ProductBatch.Product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault() ?? "",
                BestBeforeDate = sa.ProductBatch.BestBeforeDate,
                Quantity = sa.Quantity,
                Type = sa.Type,
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
            .ThenInclude(p => p.Barcodes)
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
            ProductBarcode = action.ProductBatch.Product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault() ?? "",
            BestBeforeDate = action.ProductBatch.BestBeforeDate,
            Quantity = action.Quantity,
            Type = action.Type,
            ActionAt = action.ActionAt
        };

        return Ok(dto);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchShelvingResponse>> CreateBatch([FromBody] BatchShelvingRequest request)
    {
        var response = new BatchShelvingResponse();

        foreach (var action in request.Actions)
        {
            try
            {
                // Find product by barcode through ProductBarcodes table
                var productBarcode = await _context.ProductBarcodes
                    .Include(pb => pb.Product)
                    .ThenInclude(p => p.Batches)
                    .ThenInclude(b => b.ShelvingActions)
                    .FirstOrDefaultAsync(pb => pb.Barcode == action.ProductBarcode);

                if (productBarcode == null)
                {
                    response.Errors.Add($"Product with barcode '{action.ProductBarcode}' not found");
                    continue;
                }

                var product = productBarcode.Product;

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
                if (action.Type == ShelvingActionType.MovedToShelf)
                {
                    var storageQty = _stockCalculation.CalculateStorageQuantity(batch.ShelvingActions);

                    if (action.Quantity > storageQty)
                    {
                        response.Errors.Add($"Not enough stock in storage for product '{action.ProductBarcode}'. Available: {storageQty}");
                        continue;
                    }
                }
                else if (action.Type == ShelvingActionType.MovedFromShelf || action.Type == ShelvingActionType.RemovedFromShelf)
                {
                    var shelfQty = _stockCalculation.CalculateShelfQuantity(batch.ShelvingActions);

                    if (action.Quantity > shelfQty)
                    {
                        response.Errors.Add($"Not enough stock on shelf for product '{action.ProductBarcode}'. Available: {shelfQty}");
                        continue;
                    }
                }

                // Create the shelving action
                var shelvingAction = new ShelvingAction
                {
                    ProductBatchId = batch.Id,
                    Quantity = action.Quantity,
                    Type = action.Type,
                    ActionAt = DateTime.UtcNow
                };

                _context.ShelvingActions.Add(shelvingAction);
                await _context.SaveChangesAsync();

                response.Results.Add(new ShelvingActionDto
                {
                    Id = shelvingAction.Id,
                    ProductBatchId = batch.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductBarcode = action.ProductBarcode, // Use the scanned barcode
                    BestBeforeDate = batch.BestBeforeDate,
                    Quantity = shelvingAction.Quantity,
                    Type = shelvingAction.Type,
                    ActionAt = shelvingAction.ActionAt
                });

                _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
                    action.Type, action.Quantity, product.Name, batch.Id);
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Error processing product '{action.ProductBarcode}': {ex.Message}");
            }
        }

        if (response.Errors.Any() && !response.Results.Any())
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ShelvingActionDto>> Create([FromBody] CreateShelvingActionDto dto)
    {
        // Find product by barcode through ProductBarcodes table
        var productBarcode = await _context.ProductBarcodes
            .Include(pb => pb.Product)
            .ThenInclude(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Barcode == dto.ProductBarcode);

        if (productBarcode == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var product = productBarcode.Product;

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
        if (dto.Type == ShelvingActionType.MovedToShelf)
        {
            var storageQty = _stockCalculation.CalculateStorageQuantity(batch.ShelvingActions);

            if (dto.Quantity > storageQty)
            {
                return BadRequest(new { message = $"Not enough stock in storage. Available: {storageQty}" });
            }
        }
        else if (dto.Type == ShelvingActionType.MovedFromShelf || dto.Type == ShelvingActionType.RemovedFromShelf)
        {
            var shelfQty = _stockCalculation.CalculateShelfQuantity(batch.ShelvingActions);

            if (dto.Quantity > shelfQty)
            {
                return BadRequest(new { message = $"Not enough stock on shelf. Available: {shelfQty}" });
            }
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = dto.Type,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
            dto.Type, dto.Quantity, product.Name, batch.Id);

        var result = new ShelvingActionDto
        {
            Id = shelvingAction.Id,
            ProductBatchId = batch.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            ProductBarcode = dto.ProductBarcode, // Use the scanned barcode
            BestBeforeDate = batch.BestBeforeDate,
            Quantity = shelvingAction.Quantity,
            Type = shelvingAction.Type,
            ActionAt = shelvingAction.ActionAt
        };

        return CreatedAtAction(nameof(GetById), new { id = shelvingAction.Id }, result);
    }
}
