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
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;
    private readonly IStockCalculationService _stockCalculation;
    private readonly IProductCatalogService _catalog;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger, IStockCalculationService stockCalculation, IProductCatalogService catalog)
    {
        _context = context;
        _logger = logger;
        _stockCalculation = stockCalculation;
        _catalog = catalog;
    }

    // The picture bytes stay out of product queries; only whether there is one
    private async Task<T> WithImageFlags<T>(T dtos) where T : IEnumerable<ProductDto>
    {
        var withImage = (await _context.ProductImages.Select(i => i.ProductId).ToListAsync()).ToHashSet();
        foreach (var dto in dtos) dto.HasImage = withImage.Contains(dto.Id);
        return dtos;
    }

    private async Task<ProductDto> WithImageFlag(ProductDto dto) => (await WithImageFlags(new[] { dto }))[0];

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _context.Products
            .Include(p => p.Barcodes)
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .ToListAsync();

        return Ok(await WithImageFlags(products.ToDtoList(_stockCalculation)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
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

        return Ok(await WithImageFlag(product.ToDto(_stockCalculation)));
    }

    private static bool ValidSlot(int? slot) => slot is null or (>= 1 and <= 9 and not 2);

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        if (!ValidSlot(dto.ShelfSlot)) return BadRequest(new { message = "Compartment must be 1-9 except 2 (the tablet's)" });

        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            ShelfSlot = dto.ShelfSlot,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product.ToDto(null));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
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

        if (!ValidSlot(dto.ShelfSlot)) return BadRequest(new { message = "Compartment must be 1-9 except 2 (the tablet's)" });
        product.Name = dto.Name;
        product.Price = dto.Price;
        product.ShelfSlot = dto.ShelfSlot;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId} - {ProductName}", product.Id, product.Name);

        return Ok(await WithImageFlag(product.ToDto(_stockCalculation)));
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
            .Include(pb => pb.Product)
                .ThenInclude(p => p.Batches)
                .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode);

        if (productBarcode == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        return Ok(await WithImageFlag(productBarcode.Product.ToDto(_stockCalculation)));
    }

    /// <summary>
    /// A scanned product barcode, unknown ones included: looked up online and assigned to a
    /// matching product or turned into a new one. Used by the bulk shelving and inventory scans.
    /// </summary>
    [HttpPost("resolve-barcode")]
    public async Task<ActionResult<BarcodeResolveResultDto>> ResolveBarcode([FromBody] BarcodeResolveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode)) return BadRequest(new { message = "Barcode is required" });
        if (request.Barcode.Trim().Length > 50) return BadRequest(new { message = "Barcode is too long" });

        return Ok(await _catalog.ResolveAsync(request));
    }

    // Anonymous: the tablet shows these pictures to everyone in front of the shelf
    [HttpGet("{id}/image")]
    [AllowAnonymous]
    public async Task<ActionResult> GetImage(int id)
    {
        var image = await _context.ProductImages.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == id);
        // Rows written before the type allowlist, or by hand, are not trusted to be inert
        if (image == null || !ProductCatalogService.IsAllowedImageType(image.ContentType)) return NotFound();

        Response.Headers.CacheControl = "public, max-age=3600";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        return File(image.Data, image.ContentType, lastModified: image.UpdatedAt, entityTag: null);
    }

    [HttpPut("{id}/image")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult> UploadImage(int id, IFormFile file)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id)) return NotFound(new { message = "Product not found" });
        if (!ProductCatalogService.IsAllowedImageType(file.ContentType) || file.Length > 2 * 1024 * 1024)
            return BadRequest(new { message = "Please upload a JPEG, PNG, WebP or GIF picture of at most 2 MB" });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var image = await _context.ProductImages.FindAsync(id);
        if (image == null)
        {
            _context.ProductImages.Add(new ProductImage { ProductId = id, Data = stream.ToArray(), ContentType = file.ContentType, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            image.Data = stream.ToArray();
            image.ContentType = file.ContentType;
            image.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Pictures to choose from: Open Food Facts, and Google Images when a SerpApi key is set.</summary>
    [HttpGet("{id}/image/suggestions")]
    public async Task<ActionResult<ImageSuggestionsDto>> SuggestImages(int id, [FromServices] IProductImageSearch imageSearch)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id)) return NotFound(new { message = "Product not found" });
        return Ok(await imageSearch.SuggestAsync(id));
    }

    /// <summary>Downloads a picture from an address - a suggestion or one the admin pasted.</summary>
    [HttpPost("{id}/image/from-url")]
    public async Task<ActionResult> ImageFromUrl(int id, [FromBody] ImageFromUrlRequest request)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id)) return NotFound(new { message = "Product not found" });

        return await _catalog.DownloadImageAsync(id, request.Url.Trim())
            ? NoContent()
            : BadRequest(new { message = "Could not load a JPEG, PNG, WebP or GIF picture of at most 2 MB from that address" });
    }

    [HttpDelete("{id}/image")]
    public async Task<ActionResult> DeleteImage(int id)
    {
        var image = await _context.ProductImages.FindAsync(id);
        if (image != null)
        {
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        return NoContent();
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
            Barcodes = product.Barcodes.ToDtoList(),
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
                Barcodes = product.Barcodes.ToDtoList(),
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

        return Ok(productBarcode.ToDto());
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

        return Ok(productBarcode.ToDto());
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
