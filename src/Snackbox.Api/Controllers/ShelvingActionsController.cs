using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
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
    private readonly IProductBestBeforeDateService _bestBeforeDateService;

    public ShelvingActionsController(ApplicationDbContext context, ILogger<ShelvingActionsController> logger, IStockCalculationService stockCalculation, IProductBestBeforeDateService bestBeforeDateService)
    {
        _context = context;
        _logger = logger;
        _stockCalculation = stockCalculation;
        _bestBeforeDateService = bestBeforeDateService;
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
            query = query.Where(sa => sa.ProductBatch != null && sa.ProductBatch.ProductId == productId.Value);
        }

        // Exclude consumed items without product batches from general queries
        query = query.Where(sa => sa.ProductBatch != null);

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
                ProductId = sa.ProductBatch!.ProductId,
                ProductName = sa.ProductBatch!.Product.Name,
                ProductBarcode = sa.ProductBatch!.Product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault() ?? "",
                BestBeforeDate = sa.ProductBatch!.BestBeforeDate,
                Quantity = sa.Quantity,
                Type = sa.Type,
                ActionAt = sa.ActionAt,
                InvoiceItemId = sa.InvoiceItemId
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

        return Ok(action.ToDtoWithProduct());
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchShelvingResponse>> CreateBatch([FromBody] BatchShelvingRequest request)
    {
        var response = new BatchShelvingResponse();
        var productsToUpdate = new HashSet<int>();

        foreach (var action in request.Actions)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(action.ProductBarcode) && !action.ProductId.HasValue)
                {
                    response.Errors.Add("Either ProductBarcode or ProductId must be provided");
                    continue;
                }

                Product? product = null;
                string? barcode = null;

                // Find product by ProductId or barcode
                if (action.ProductId.HasValue)
                {
                    product = await _context.Products
                        .Include(p => p.Batches)
                        .ThenInclude(b => b.ShelvingActions)
                        .Include(p => p.Barcodes)
                        .FirstOrDefaultAsync(p => p.Id == action.ProductId.Value);

                    if (product == null)
                    {
                        response.Errors.Add($"Product with ID '{action.ProductId}' not found");
                        continue;
                    }

                    barcode = product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(action.ProductBarcode))
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

                    product = productBarcode.Product;
                    barcode = action.ProductBarcode;
                }

                if (product == null)
                {
                    response.Errors.Add("Product not found");
                    continue;
                }

                // Check if this is for an invoice item and if it's already processed
                if (action.InvoiceItemId.HasValue)
                {
                    var invoiceItem = await _context.InvoiceItems.FindAsync(action.InvoiceItemId.Value);
                    if (invoiceItem != null && (invoiceItem.Status == InvoiceItemStatus.Processed ||
                        await _context.ShelvingActions.AnyAsync(sa => sa.InvoiceItemId == action.InvoiceItemId.Value)))
                    {
                        response.Errors.Add($"Invoice item {action.InvoiceItemId} has already been processed");
                        continue;
                    }
                }

                // Handle consumed items separately - no batch needed
                if (action.Type == ShelvingActionType.Consumed)
                {
                    // Create a consumed shelving action without a batch
                    var consumedAction = new ShelvingAction
                    {
                        ProductBatchId = null, // No batch for consumed items
                        Quantity = action.Quantity,
                        Type = action.Type,
                        ActionAt = DateTime.UtcNow,
                        InvoiceItemId = action.InvoiceItemId
                    };

                    _context.ShelvingActions.Add(consumedAction);

                    // Update invoice item status to Processed if applicable
                    if (action.InvoiceItemId.HasValue)
                    {
                        var invoiceItem = await _context.InvoiceItems.FindAsync(action.InvoiceItemId.Value);
                        if (invoiceItem != null)
                        {
                            invoiceItem.Status = InvoiceItemStatus.Processed;
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Marking {Quantity} of {ProductName} as consumed", action.Quantity, product.Name);

                    // Add to response
                    consumedAction.ProductBatch = new ProductBatch { Product = product, BestBeforeDate = DateTime.UtcNow };
                    response.Results.Add(consumedAction.ToDtoWithBarcode(barcode ?? ""));
                    continue;
                }

                // Validate best before date is provided for non-consumed items
                if (!action.BestBeforeDate.HasValue)
                {
                    response.Errors.Add($"Best before date is required for product '{product.Name}'");
                    continue;
                }

                // Find or create batch for the given best-before date
                var batch = product.Batches.FirstOrDefault(b => b.BestBeforeDate.Date == action.BestBeforeDate.Value.Date);

                if (batch == null)
                {
                    // Create new batch if not exists
                    batch = new ProductBatch
                    {
                        ProductId = product.Id,
                        BestBeforeDate = action.BestBeforeDate.Value.Date,
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
                        response.Errors.Add($"Not enough stock in storage for product '{product.Name}'. Available: {storageQty}");
                        continue;
                    }
                }
                else if (action.Type == ShelvingActionType.MovedFromShelf || action.Type == ShelvingActionType.RemovedFromShelf)
                {
                    var shelfQty = _stockCalculation.CalculateShelfQuantity(batch.ShelvingActions);

                    if (action.Quantity > shelfQty)
                    {
                        response.Errors.Add($"Not enough stock on shelf for product '{product.Name}'. Available: {shelfQty}");
                        continue;
                    }
                }

                // Create the shelving action
                var shelvingAction = action.ToEntity(batch.Id);

                _context.ShelvingActions.Add(shelvingAction);

                // Update invoice item status to Processed if applicable
                if (action.InvoiceItemId.HasValue)
                {
                    var invoiceItem = await _context.InvoiceItems.FindAsync(action.InvoiceItemId.Value);
                    if (invoiceItem != null)
                    {
                        invoiceItem.Status = InvoiceItemStatus.Processed;
                    }
                }

                await _context.SaveChangesAsync();

                // Track product for best before date update
                productsToUpdate.Add(product.Id);

                shelvingAction.ProductBatch = batch;
                batch.Product = product;
                response.Results.Add(shelvingAction.ToDtoWithBarcode(barcode ?? ""));

                _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
                    action.Type, action.Quantity, product.Name, batch.Id);
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Error processing product: {ex.Message}");
            }
        }

        // Update best before dates for all affected products
        foreach (var productId in productsToUpdate)
        {
            await _bestBeforeDateService.UpdateProductBestBeforeDatesAsync(productId);
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
        Product? product = null;
        string? barcode = null;

        // Find product by ProductId or barcode
        if (dto.ProductId.HasValue)
        {
            product = await _context.Products
                .Include(p => p.Batches)
                .ThenInclude(b => b.ShelvingActions)
                .Include(p => p.Barcodes)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId.Value);

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Get first barcode for the product
            barcode = product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault();
        }
        else if (!string.IsNullOrEmpty(dto.ProductBarcode))
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

            product = productBarcode.Product;
            barcode = dto.ProductBarcode;
        }
        else
        {
            return BadRequest(new { message = "Either ProductId or ProductBarcode must be provided" });
        }

        // Check if this is for an invoice item and if it's already processed
        if (dto.InvoiceItemId.HasValue)
        {
            var invoiceItem = await _context.InvoiceItems.FindAsync(dto.InvoiceItemId.Value);
            if (invoiceItem != null && (invoiceItem.Status == InvoiceItemStatus.Processed ||
                await _context.ShelvingActions.AnyAsync(sa => sa.InvoiceItemId == dto.InvoiceItemId.Value)))
            {
                return BadRequest(new { message = "Invoice item has already been processed. Cannot add to stock again." });
            }
        }

        // Validate best before date is provided for storage/shelf actions
        if ((dto.Type == ShelvingActionType.AddedToStorage || dto.Type == ShelvingActionType.AddedToShelf || dto.Type == ShelvingActionType.MovedToShelf)
            && !dto.BestBeforeDate.HasValue)
        {
            return BadRequest(new { message = "Best before date is required for adding to storage or shelf" });
        }

        // For consumed items, create a shelving action but no batch
        if (dto.Type == ShelvingActionType.Consumed)
        {
            // Create a consumed shelving action without a batch
            var consumedAction = new ShelvingAction
            {
                ProductBatchId = null, // No batch for consumed items
                Quantity = dto.Quantity,
                Type = dto.Type,
                ActionAt = DateTime.UtcNow,
                InvoiceItemId = dto.InvoiceItemId
            };

            _context.ShelvingActions.Add(consumedAction);

            // Update invoice item status to Processed if applicable
            if (dto.InvoiceItemId.HasValue)
            {
                var invoiceItem = await _context.InvoiceItems.FindAsync(dto.InvoiceItemId.Value);
                if (invoiceItem != null)
                {
                    invoiceItem.Status = InvoiceItemStatus.Processed;
                    _logger.LogInformation("Invoice item {InvoiceItemId} marked as consumed without creating stock", dto.InvoiceItemId);
                }
            }

            await _context.SaveChangesAsync();

            // Return a response indicating the item was marked as consumed
            return Ok(new ShelvingActionDto
            {
                Id = consumedAction.Id,
                ProductBatchId = 0,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductBarcode = barcode ?? "",
                BestBeforeDate = DateTime.UtcNow,
                Quantity = dto.Quantity,
                Type = dto.Type,
                ActionAt = DateTime.UtcNow,
                InvoiceItemId = dto.InvoiceItemId
            });
        }

        // Find or create batch for the given best-before date
        var batch = product.Batches.FirstOrDefault(b => b.BestBeforeDate.Date == dto.BestBeforeDate!.Value.Date);

        if (batch == null)
        {
            batch = new ProductBatch
            {
                ProductId = product.Id,
                BestBeforeDate = dto.BestBeforeDate.Value.Date,
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

        var shelvingAction = dto.ToEntity(batch.Id);

        _context.ShelvingActions.Add(shelvingAction);

        // Update invoice item status to Processed if applicable
        if (dto.InvoiceItemId.HasValue)
        {
            var invoiceItem = await _context.InvoiceItems.FindAsync(dto.InvoiceItemId.Value);
            if (invoiceItem != null)
            {
                invoiceItem.Status = InvoiceItemStatus.Processed;
            }
        }

        await _context.SaveChangesAsync();

        // Update product best before dates
        await _bestBeforeDateService.UpdateProductBestBeforeDatesAsync(product.Id);

        _logger.LogInformation("Shelving action created: {ActionType} {Quantity} of {ProductName} (Batch {BatchId})",
            dto.Type, dto.Quantity, product.Name, batch.Id);

        shelvingAction.ProductBatch = batch;
        batch.Product = product;
        var result = shelvingAction.ToDtoWithBarcode(barcode ?? "");

        return CreatedAtAction(nameof(GetById), new { id = shelvingAction.Id }, result);
    }
}
