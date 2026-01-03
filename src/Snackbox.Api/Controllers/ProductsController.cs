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
            .Include(p => p.Barcodes)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CreatedAt = p.CreatedAt,
                BestBeforeInStock = p.BestBeforeInStock,
                BestBeforeOnShelf = p.BestBeforeOnShelf,
                Barcodes = p.Barcodes.Select(b => new ProductBarcodeDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    Barcode = b.Barcode,
                    Quantity = b.Quantity,
                    CreatedAt = b.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
            Barcodes = product.Barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                ProductId = b.ProductId,
                Barcode = b.Barcode,
                Quantity = b.Quantity,
                CreatedAt = b.CreatedAt
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var currentPrimaryBarcode = product.Barcodes.OrderBy(b => b.Id).FirstOrDefault();

        // Check if new barcode conflicts with another product
        if (currentPrimaryBarcode != null &&
            dto.Barcode != currentPrimaryBarcode.Barcode &&
            await _context.ProductBarcodes.AnyAsync(pb => pb.Barcode == dto.Barcode))
        {
            return BadRequest(new { message = "A product with this barcode already exists" });
        }

        product.Name = dto.Name;

        // Update the primary barcode if it changed
        if (currentPrimaryBarcode != null && currentPrimaryBarcode.Barcode != dto.Barcode)
        {
            currentPrimaryBarcode.Barcode = dto.Barcode;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId} - {ProductName}", product.Id, product.Name);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
            Barcodes = product.Barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                ProductId = b.ProductId,
                Barcode = b.Barcode,
                Quantity = b.Quantity,
                CreatedAt = b.CreatedAt
            }).ToList()
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
        var productBarcode = await _context.ProductBarcodes
            .Include(pb => pb.Product)
            .ThenInclude(p => p.Barcodes)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode);

        if (productBarcode == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var product = productBarcode.Product;

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            BestBeforeInStock = product.BestBeforeInStock,
            BestBeforeOnShelf = product.BestBeforeOnShelf,
            Barcodes = product.Barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                ProductId = b.ProductId,
                Barcode = b.Barcode,
                Quantity = b.Quantity,
                CreatedAt = b.CreatedAt
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<ProductStockDto>> GetProductStock(int id)
    {
        var product = await _context.Products
            .Include(p => p.Barcodes)
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
            ProductBarcode = product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault() ?? "",
            Barcodes = product.Barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                ProductId = b.ProductId,
                Barcode = b.Barcode,
                Quantity = b.Quantity,
                CreatedAt = b.CreatedAt
            }).ToList(),
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
            .Include(p => p.Barcodes)
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
                ProductBarcode = product.Barcodes.OrderBy(b => b.Id).Select(b => b.Barcode).FirstOrDefault() ?? "",
                Barcodes = product.Barcodes.Select(b => new ProductBarcodeDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    Barcode = b.Barcode,
                    Quantity = b.Quantity,
                    CreatedAt = b.CreatedAt
                }).ToList(),
                TotalInStorage = batches.Sum(b => b.QuantityInStorage),
                TotalOnShelf = batches.Sum(b => b.QuantityOnShelf),
                Batches = batches
            };
        }).ToList();

        return Ok(result);
    }

    // Barcode management endpoints
    [HttpPost("{id}/barcodes")]
    public async Task<ActionResult<ProductBarcodeDto>> AddBarcode(int id, [FromBody] CreateProductBarcodeDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        // Check if barcode already exists
        if (await _context.ProductBarcodes.AnyAsync(pb => pb.Barcode == dto.Barcode))
        {
            return BadRequest(new { message = "This barcode already exists" });
        }

        var productBarcode = new ProductBarcode
        {
            ProductId = id,
            Barcode = dto.Barcode,
            Quantity = dto.Quantity,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductBarcodes.Add(productBarcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode added: {Barcode} to product {ProductId}", dto.Barcode, id);

        var resultDto = new ProductBarcodeDto
        {
            Id = productBarcode.Id,
            ProductId = productBarcode.ProductId,
            Barcode = productBarcode.Barcode,
            Quantity = productBarcode.Quantity,
            CreatedAt = productBarcode.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpPut("{productId}/barcodes/{barcodeId}")]
    public async Task<ActionResult<ProductBarcodeDto>> UpdateBarcode(int productId, int barcodeId, [FromBody] UpdateProductBarcodeDto dto)
    {
        var productBarcode = await _context.ProductBarcodes
            .FirstOrDefaultAsync(pb => pb.Id == barcodeId && pb.ProductId == productId);

        if (productBarcode == null)
        {
            return NotFound(new { message = "Product barcode not found" });
        }

        // Check if new barcode conflicts with another barcode
        if (dto.Barcode != productBarcode.Barcode &&
            await _context.ProductBarcodes.AnyAsync(pb => pb.Barcode == dto.Barcode))
        {
            return BadRequest(new { message = "This barcode already exists" });
        }

        productBarcode.Barcode = dto.Barcode;
        productBarcode.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode updated: {BarcodeId} for product {ProductId}", barcodeId, productId);

        var resultDto = new ProductBarcodeDto
        {
            Id = productBarcode.Id,
            ProductId = productBarcode.ProductId,
            Barcode = productBarcode.Barcode,
            Quantity = productBarcode.Quantity,
            CreatedAt = productBarcode.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpDelete("{productId}/barcodes/{barcodeId}")]
    public async Task<ActionResult> DeleteBarcode(int productId, int barcodeId)
    {
        var productBarcode = await _context.ProductBarcodes
            .FirstOrDefaultAsync(pb => pb.Id == barcodeId && pb.ProductId == productId);

        if (productBarcode == null)
        {
            return NotFound(new { message = "Product barcode not found" });
        }

        // Check if this is the last barcode
        var barcodeCount = await _context.ProductBarcodes.CountAsync(pb => pb.ProductId == productId);
        if (barcodeCount <= 1)
        {
            return BadRequest(new { message = "Cannot delete the last barcode. Product must have at least one barcode." });
        }

        _context.ProductBarcodes.Remove(productBarcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode deleted: {BarcodeId} from product {ProductId}", barcodeId, productId);

        return NoContent();
    }
}
