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
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetAllProducts");
        
        _logger.LogInformation("Fetching all products");
        
        var products = await _context.Products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Price = p.Price,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {ProductCount} products", products.Count);
        activity?.SetTag("products.count", products.Count);

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetProductById");
        activity?.SetTag("product.id", id);
        
        _logger.LogInformation("Fetching product by ID. ProductId: {ProductId}", id);
        
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            _logger.LogWarning("Product not found. ProductId: {ProductId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            return NotFound(new { message = "Product not found" });
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            Price = product.Price,
            Description = product.Description,
            CreatedAt = product.CreatedAt
        };

        _logger.LogInformation(
            "Product retrieved. ProductId: {ProductId}, Name: {ProductName}, Price: {Price}",
            product.Id,
            product.Name,
            product.Price);
        
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.price", product.Price);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CreateProduct");
        activity?.SetTag("product.name", dto.Name);
        activity?.SetTag("product.barcode", dto.Barcode);
        activity?.SetTag("product.price", dto.Price);
        
        _logger.LogInformation(
            "Creating product. Name: {ProductName}, Barcode: {Barcode}, Price: {Price}, Description: {Description}",
            dto.Name,
            dto.Barcode,
            dto.Price,
            dto.Description ?? "N/A");

        // Check if barcode already exists
        if (await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            _logger.LogWarning(
                "Product creation failed: Barcode already exists. Barcode: {Barcode}",
                dto.Barcode);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode already exists");
            return BadRequest(new { message = "A product with this barcode already exists" });
        }

        var product = new Product
        {
            Name = dto.Name,
            Barcode = dto.Barcode,
            Price = dto.Price,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product created successfully. ProductId: {ProductId}, Name: {ProductName}, Barcode: {Barcode}, Price: {Price}",
            product.Id,
            product.Name,
            product.Barcode,
            product.Price);
        
        SnackboxTelemetry.ProductCreatedCounter.Add(1);
        activity?.SetTag("product.id", product.Id);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            Price = product.Price,
            Description = product.Description,
            CreatedAt = product.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("UpdateProduct");
        activity?.SetTag("product.id", id);
        activity?.SetTag("product.new_name", dto.Name);
        activity?.SetTag("product.new_barcode", dto.Barcode);
        activity?.SetTag("product.new_price", dto.Price);
        
        _logger.LogInformation(
            "Updating product. ProductId: {ProductId}, NewName: {NewName}, NewBarcode: {NewBarcode}, NewPrice: {NewPrice}",
            id,
            dto.Name,
            dto.Barcode,
            dto.Price);

        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            _logger.LogWarning("Product update failed: Product not found. ProductId: {ProductId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            return NotFound(new { message = "Product not found" });
        }

        var oldName = product.Name;
        var oldBarcode = product.Barcode;
        var oldPrice = product.Price;

        // Check if new barcode conflicts with another product
        if (dto.Barcode != product.Barcode && await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            _logger.LogWarning(
                "Product update failed: New barcode already exists. ProductId: {ProductId}, NewBarcode: {NewBarcode}",
                id,
                dto.Barcode);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode already exists");
            return BadRequest(new { message = "A product with this barcode already exists" });
        }

        product.Name = dto.Name;
        product.Barcode = dto.Barcode;
        product.Price = dto.Price;
        product.Description = dto.Description;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product updated successfully. ProductId: {ProductId}, OldName: {OldName} -> NewName: {NewName}, " +
            "OldBarcode: {OldBarcode} -> NewBarcode: {NewBarcode}, OldPrice: {OldPrice} -> NewPrice: {NewPrice}",
            product.Id,
            oldName,
            product.Name,
            oldBarcode,
            product.Barcode,
            oldPrice,
            product.Price);

        var resultDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Barcode = product.Barcode,
            Price = product.Price,
            Description = product.Description,
            CreatedAt = product.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("DeleteProduct");
        activity?.SetTag("product.id", id);
        
        _logger.LogInformation("Attempting to delete product. ProductId: {ProductId}", id);

        var product = await _context.Products
            .Include(p => p.Batches)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            _logger.LogWarning("Product deletion failed: Product not found. ProductId: {ProductId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            return NotFound(new { message = "Product not found" });
        }

        if (product.Batches.Any())
        {
            _logger.LogWarning(
                "Product deletion failed: Product has {BatchCount} existing batches. ProductId: {ProductId}, ProductName: {ProductName}",
                product.Batches.Count,
                product.Id,
                product.Name);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Product has existing batches");
            activity?.SetTag("product.batch_count", product.Batches.Count);
            return BadRequest(new { message = "Cannot delete product with existing batches. Delete batches first." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product deleted successfully. ProductId: {ProductId}, ProductName: {ProductName}",
            product.Id,
            product.Name);

        return NoContent();
    }
}
