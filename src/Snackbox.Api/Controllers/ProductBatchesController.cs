using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

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
                    .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.AddedToShelf)
                    .Sum(sa => sa.Quantity) - pb.ShelvingActions
                    .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
                    .Sum(sa => sa.Quantity),
                CreatedAt = pb.CreatedAt
            })
            .ToListAsync();

        return Ok(batches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductBatchDto>> GetById(int id)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductBatchDto>> Create([FromBody] CreateProductBatchDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
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

        _logger.LogInformation("Product batch created: {BatchId} for product {ProductId}", batch.Id, batch.ProductId);

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
        var batch = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        batch.BestBeforeDate = dto.BestBeforeDate;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product batch updated: {BatchId}", batch.Id);

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
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        var storageQty = batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.AddedToStorage || sa.Type == ShelvingActionType.MovedFromShelf)
            .Sum(sa => sa.Quantity) - batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.RemovedFromStorage)
            .Sum(sa => sa.Quantity);

        if (dto.Quantity > storageQty)
        {
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

        _logger.LogInformation("Moved {Quantity} items from storage to shelf for batch {BatchId}", dto.Quantity, batch.Id);

        return Ok(new { message = $"Moved {dto.Quantity} items to shelf" });
    }

    [HttpPost("{id}/move-to-storage")]
    public async Task<ActionResult> MoveToStorage(int id, [FromBody] MoveStockDto dto)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        var shelfQty = batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf)
            .Sum(sa => sa.Quantity) - batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf)
            .Sum(sa => sa.Quantity);

        if (dto.Quantity > shelfQty)
        {
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

        _logger.LogInformation("Moved {Quantity} items from shelf to storage for batch {BatchId}", dto.Quantity, batch.Id);

        return Ok(new { message = $"Moved {dto.Quantity} items to storage" });
    }

    [HttpPost("{id}/add-to-storage")]
    public async Task<ActionResult> AddToStorage(int id, [FromBody] MoveStockDto dto)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = ShelvingActionType.AddedToStorage,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Quantity} items to storage for batch {BatchId}", dto.Quantity, batch.Id);

        return Ok(new { message = $"Added {dto.Quantity} items to storage" });
    }

    [HttpPost("{id}/add-to-shelf")]
    public async Task<ActionResult> AddToShelf(int id, [FromBody] MoveStockDto dto)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        // Add directly to shelf with a single action
        var addToShelfAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = ShelvingActionType.AddedToShelf,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(addToShelfAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Quantity} items directly to shelf for batch {BatchId}", dto.Quantity, batch.Id);

        return Ok(new { message = $"Added {dto.Quantity} items to shelf" });
    }

    [HttpPost("{id}/consume")]
    public async Task<ActionResult> ConsumeFromShelf(int id, [FromBody] MoveStockDto dto)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        var shelfQty = batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedToShelf || sa.Type == ShelvingActionType.AddedToShelf)
            .Sum(sa => sa.Quantity) - batch.ShelvingActions
            .Where(sa => sa.Type == ShelvingActionType.MovedFromShelf || sa.Type == ShelvingActionType.RemovedFromShelf || sa.Type == ShelvingActionType.Consumed)
            .Sum(sa => sa.Quantity);

        if (dto.Quantity > shelfQty)
        {
            return BadRequest(new { message = $"Not enough stock on shelf. Available: {shelfQty}" });
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = dto.Quantity,
            Type = ShelvingActionType.Consumed,
            ActionAt = DateTime.UtcNow
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked {Quantity} items as consumed from shelf for batch {BatchId}", dto.Quantity, batch.Id);

        return Ok(new { message = $"Marked {dto.Quantity} items as consumed" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var batch = await _context.ProductBatches
            .Include(pb => pb.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound(new { message = "Product batch not found" });
        }

        _context.ProductBatches.Remove(batch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product batch deleted: {BatchId}", batch.Id);

        return NoContent();
    }
}
