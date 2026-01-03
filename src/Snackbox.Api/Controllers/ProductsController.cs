using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;
    private readonly IStockCalculationService _stockCalculation;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger, IStockCalculationService stockCalculation)
    {
        _context = context;
        _logger = logger;
        _stockCalculation = stockCalculation;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _context.Products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                CreatedAt = p.CreatedAt,
                BestBeforeInStock = p.BestBeforeInStock,
                BestBeforeOnShelf = p.BestBeforeOnShelf,
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        // Check if barcode already exists
        if (await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            return BadRequest(new { message = "A product with this barcode already exists" });
        }

        var product = new Product
        {
            Name = dto.Name,
            Barcode = dto.Barcode,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        // Check if new barcode conflicts with another product
        if (dto.Barcode != product.Barcode && await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            return BadRequest(new { message = "A product with this barcode already exists" });
        }

        product.Name = dto.Name;
        product.Barcode = dto.Barcode;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId} - {ProductName}", product.Id, product.Name);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
        };

        return Ok(resultDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Batches)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        if (product.Batches.Any())
        {
            return BadRequest(new { message = "Cannot delete product with existing batches. Delete batches first." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId} - {ProductName}", product.Id, product.Name);

        return NoContent();
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
        };

        return Ok(dto);
    }

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<ProductStockDto>> GetProductStock(int id)
    {
        var product = await _context.Products
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var batches = product.Batches.Select(b => new BatchStockInfo
        {
            BatchId = b.Id,
            BestBeforeDate = b.BestBeforeDate,
            QuantityInStorage = _stockCalculation.CalculateStorageQuantity(b.ShelvingActions),
            QuantityOnShelf = _stockCalculation.CalculateShelfQuantity(b.ShelvingActions)
        }).ToList();

        var dto = new ProductStockDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductBarcode = product.Barcode,
            TotalInStorage = batches.Sum(b => b.QuantityInStorage),
            TotalOnShelf = batches.Sum(b => b.QuantityOnShelf),
            Batches = batches
        };

        return Ok(dto);
    }

    [HttpGet("stock")]
    public async Task<ActionResult<IEnumerable<ProductStockDto>>> GetAllProductStock()
    {
        var products = await _context.Products
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .ToListAsync();

        var result = products.Select(product =>
        {
            var batches = product.Batches.Select(b => new BatchStockInfo
            {
                BatchId = b.Id,
                BestBeforeDate = b.BestBeforeDate,
                QuantityInStorage = _stockCalculation.CalculateStorageQuantity(b.ShelvingActions),
                QuantityOnShelf = _stockCalculation.CalculateShelfQuantity(b.ShelvingActions)
            }).ToList();

            return new ProductStockDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductBarcode = product.Barcode,
                TotalInStorage = batches.Sum(b => b.QuantityInStorage),
                TotalOnShelf = batches.Sum(b => b.QuantityOnShelf),
                Batches = batches
            };
        }).ToList();

        return Ok(result);
    }
}
