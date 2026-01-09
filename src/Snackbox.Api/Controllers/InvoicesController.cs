using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using System.Security.Claims;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class InvoicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoicesController> _logger;
    private readonly InvoiceParserFactory _parserFactory;
    private readonly IProductMatchingService _productMatching;

    public InvoicesController(
        ApplicationDbContext context, 
        ILogger<InvoicesController> logger,
        InvoiceParserFactory parserFactory,
        IProductMatchingService productMatching)
    {
        _context = context;
        _logger = logger;
        _parserFactory = parserFactory;
        _productMatching = productMatching;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
    {
        var invoices = await _context.Invoices
            .Include(i => i.CreatedBy)
            .Include(i => i.PaidBy)
            .Include(i => i.Items)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                Supplier = i.Supplier,
                TotalAmount = i.TotalAmount,
                AdditionalCosts = i.AdditionalCosts,
                PriceReduction = i.PriceReduction,
                PaidByUserId = i.PaidByUserId,
                PaidByUsername = i.PaidBy.Username,
                PaymentId = i.PaymentId,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt,
                CreatedByUserId = i.CreatedByUserId,
                CreatedByUsername = i.CreatedBy.Username,
                Items = i.Items.Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    InvoiceId = item.InvoiceId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    BestBeforeDate = item.BestBeforeDate,
                    Notes = item.Notes,
                    ArticleNumber = item.ArticleNumber
                }).ToList()
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.CreatedBy)
            .Include(i => i.PaidBy)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Invoice not found" });
        }

        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Supplier = invoice.Supplier,
            TotalAmount = invoice.TotalAmount,
            AdditionalCosts = invoice.AdditionalCosts,
            PriceReduction = invoice.PriceReduction,
            PaidByUserId = invoice.PaidByUserId,
            PaidByUsername = invoice.PaidBy.Username,
            PaymentId = invoice.PaymentId,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUsername = invoice.CreatedBy.Username,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                BestBeforeDate = item.BestBeforeDate,
                Notes = item.Notes,
                ArticleNumber = item.ArticleNumber
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user" });
        }

        var invoice = new Invoice
        {
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = DateTime.SpecifyKind(dto.InvoiceDate, DateTimeKind.Utc),
            Supplier = dto.Supplier,
            AdditionalCosts = dto.AdditionalCosts,
            PriceReduction = dto.PriceReduction,
            PaidByUserId = dto.PaidByUserId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        // Add items
        foreach (var itemDto in dto.Items)
        {
            var item = new InvoiceItem
            {
                ProductName = itemDto.ProductName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = itemDto.Quantity * itemDto.UnitPrice,
                BestBeforeDate = itemDto.BestBeforeDate.HasValue 
                    ? DateTime.SpecifyKind(itemDto.BestBeforeDate.Value, DateTimeKind.Utc) 
                    : null,
                Notes = itemDto.Notes,
                ArticleNumber = itemDto.ArticleNumber
            };
            invoice.Items.Add(item);
        }

        // Calculate total amount (items + additional costs - price reduction)
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice) + invoice.AdditionalCosts - invoice.PriceReduction;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Create payment for the invoice
        var payment = new Payment
        {
            UserId = invoice.PaidByUserId,
            Amount = invoice.TotalAmount,
            PaidAt = invoice.InvoiceDate,
            Notes = $"Payment for invoice {invoice.InvoiceNumber}",
            Type = PaymentType.CashRegister,
            InvoiceId = invoice.Id
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Update invoice with payment reference
        invoice.PaymentId = payment.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created: {InvoiceNumber} with {ItemCount} items and payment {PaymentId}", 
            invoice.InvoiceNumber, invoice.Items.Count, payment.Id);

        // Load the created by and paid by users for the response
        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();
        await _context.Entry(invoice).Reference(i => i.PaidBy).LoadAsync();

        var result = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Supplier = invoice.Supplier,
            TotalAmount = invoice.TotalAmount,
            AdditionalCosts = invoice.AdditionalCosts,
            PriceReduction = invoice.PriceReduction,
            PaidByUserId = invoice.PaidByUserId,
            PaidByUsername = invoice.PaidBy.Username,
            PaymentId = invoice.PaymentId,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUsername = invoice.CreatedBy.Username,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                BestBeforeDate = item.BestBeforeDate,
                Notes = item.Notes,
                ArticleNumber = item.ArticleNumber
            }).ToList()
        };

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InvoiceDto>> Update(int id, [FromBody] UpdateInvoiceDto dto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.CreatedBy)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Invoice not found" });
        }

        invoice.InvoiceNumber = dto.InvoiceNumber;
        invoice.InvoiceDate = dto.InvoiceDate;
        invoice.Supplier = dto.Supplier;
        invoice.AdditionalCosts = dto.AdditionalCosts;
        invoice.Notes = dto.Notes;

        // Recalculate total amount
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice) + invoice.AdditionalCosts;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice updated: {InvoiceId} - {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        var result = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Supplier = invoice.Supplier,
            TotalAmount = invoice.TotalAmount,
            AdditionalCosts = invoice.AdditionalCosts,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUsername = invoice.CreatedBy.Username,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                BestBeforeDate = item.BestBeforeDate,
                Notes = item.Notes,
                ArticleNumber = item.ArticleNumber
            }).ToList()
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.ShelvingActions)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Invoice not found" });
        }

        // Check if any invoice items are referenced by shelving actions
        if (invoice.Items.Any(item => item.ShelvingActions.Any()))
        {
            return BadRequest(new { message = "Cannot delete invoice with items that have been added to stock" });
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice deleted: {InvoiceId} - {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        return NoContent();
    }

    [HttpPost("parse")]
    public async Task<ActionResult<ParseInvoiceResponse>> ParseInvoice([FromBody] ParseInvoiceRequest request)
    {
        IInvoiceParserService? parser = null;
        
        // If no format specified, try to auto-detect
        if (string.IsNullOrWhiteSpace(request.Format))
        {
            parser = _parserFactory.DetectParser(request.InvoiceText);
            if (parser == null)
            {
                return BadRequest(new ParseInvoiceResponse
                {
                    Success = false,
                    ErrorMessage = $"Could not detect invoice format. Please select a format manually. Supported formats: {string.Join(", ", _parserFactory.GetSupportedFormats())}"
                });
            }
            _logger.LogInformation("Auto-detected invoice format: {Format}", parser.Format);
        }
        else
        {
            parser = _parserFactory.GetParser(request.Format);
            if (parser == null)
            {
                return BadRequest(new ParseInvoiceResponse
                {
                    Success = false,
                    ErrorMessage = $"Unsupported invoice format: {request.Format}. Supported formats: {string.Join(", ", _parserFactory.GetSupportedFormats())}"
                });
            }
        }

        var result = parser.Parse(request.InvoiceText);
        
        // Try to match each item to existing products
        foreach (var item in result.Items)
        {
            var match = await _productMatching.FindMatchingProduct(
                item.ArticleNumber ?? string.Empty, 
                item.ProductName);
            
            if (match != null)
            {
                item.MatchedProductId = match.ProductId;
                item.MatchedProductName = match.ProductName;
                item.MatchType = match.MatchType;
                item.MatchConfidence = match.Confidence;
            }
        }
        
        _logger.LogInformation("Invoice parsed: Format={Format}, Success={Success}, Items={ItemCount}, Matched={MatchedCount}", 
            parser.Format, result.Success, result.Items.Count, result.Items.Count(i => i.MatchedProductId.HasValue));

        return Ok(result);
    }

    [HttpPost("from-parsed")]
    public async Task<ActionResult<InvoiceDto>> CreateFromParsed([FromBody] CreateInvoiceFromParsedDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user" });
        }

        var invoice = new Invoice
        {
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = DateTime.SpecifyKind(dto.InvoiceDate, DateTimeKind.Utc),
            Supplier = dto.Supplier,
            AdditionalCosts = dto.AdditionalCosts,
            PriceReduction = dto.PriceReduction,
            PaidByUserId = dto.PaidByUserId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        // Add selected items
        foreach (var itemDto in dto.SelectedItems.Where(i => i.Selected))
        {
            var item = new InvoiceItem
            {
                ProductName = itemDto.ProductName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = itemDto.TotalPrice,
                BestBeforeDate = itemDto.BestBeforeDate.HasValue 
                    ? DateTime.SpecifyKind(itemDto.BestBeforeDate.Value, DateTimeKind.Utc) 
                    : null,
                ArticleNumber = itemDto.ArticleNumber
            };
            invoice.Items.Add(item);
        }

        // Calculate total amount (items + additional costs - price reduction)
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice) + invoice.AdditionalCosts - invoice.PriceReduction;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Create payment for the invoice
        var payment = new Payment
        {
            UserId = invoice.PaidByUserId,
            Amount = invoice.TotalAmount,
            PaidAt = invoice.InvoiceDate,
            Notes = $"Payment for invoice {invoice.InvoiceNumber}",
            Type = PaymentType.CashRegister,
            InvoiceId = invoice.Id
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Update invoice with payment reference
        invoice.PaymentId = payment.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created from parsed data: {InvoiceNumber} with {ItemCount} items and payment {PaymentId}", 
            invoice.InvoiceNumber, invoice.Items.Count, payment.Id);

        // Load the created by and paid by users for the response
        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();
        await _context.Entry(invoice).Reference(i => i.PaidBy).LoadAsync();

        var result = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Supplier = invoice.Supplier,
            TotalAmount = invoice.TotalAmount,
            AdditionalCosts = invoice.AdditionalCosts,
            PriceReduction = invoice.PriceReduction,
            PaidByUserId = invoice.PaidByUserId,
            PaidByUsername = invoice.PaidBy.Username,
            PaymentId = invoice.PaymentId,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            CreatedByUserId = invoice.CreatedByUserId,
            CreatedByUsername = invoice.CreatedBy.Username,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                BestBeforeDate = item.BestBeforeDate,
                Notes = item.Notes,
                ArticleNumber = item.ArticleNumber
            }).ToList()
        };

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, result);
    }

    [HttpGet("formats")]
    public ActionResult<IEnumerable<string>> GetSupportedFormats()
    {
        return Ok(_parserFactory.GetSupportedFormats());
    }

    [HttpPost("items/{itemId}/add-to-stock")]
    public async Task<ActionResult<ShelvingActionDto>> AddInvoiceItemToStock(
        int itemId, 
        [FromBody] AddInvoiceItemToStockDto dto)
    {
        var item = await _context.InvoiceItems
            .Include(i => i.Invoice)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
        {
            return NotFound(new { message = "Invoice item not found" });
        }

        // Find or create product
        int productId;
        if (dto.ProductId.HasValue)
        {
            productId = dto.ProductId.Value;
        }
        else if (!string.IsNullOrEmpty(dto.ProductBarcode))
        {
            // Find product by barcode
            var productBarcode = await _context.ProductBarcodes
                .FirstOrDefaultAsync(pb => pb.Barcode == dto.ProductBarcode);
            
            if (productBarcode != null)
            {
                productId = productBarcode.ProductId;
            }
            else
            {
                return BadRequest(new { message = "Product not found with the provided barcode" });
            }
        }
        else
        {
            return BadRequest(new { message = "Either ProductId or ProductBarcode must be provided" });
        }

        // Add article number as barcode if provided and doesn't exist
        if (!string.IsNullOrEmpty(item.ArticleNumber))
        {
            var existingBarcode = await _context.ProductBarcodes
                .AnyAsync(pb => pb.Barcode == item.ArticleNumber);
            
            if (!existingBarcode)
            {
                var newBarcode = new ProductBarcode
                {
                    ProductId = productId,
                    Barcode = item.ArticleNumber,
                    Quantity = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductBarcodes.Add(newBarcode);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Added article number {ArticleNumber} as barcode for product {ProductId}", 
                    item.ArticleNumber, productId);
            }
        }

        // Create shelving action
        var product = await _context.Products
            .Include(p => p.Batches)
            .ThenInclude(b => b.ShelvingActions)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        var bestBeforeDate = item.BestBeforeDate ?? DateTime.UtcNow.AddMonths(6);
        var batch = product.Batches.FirstOrDefault(b => b.BestBeforeDate.Date == bestBeforeDate.Date);

        if (batch == null)
        {
            batch = new ProductBatch
            {
                ProductId = product.Id,
                BestBeforeDate = bestBeforeDate.Date,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductBatches.Add(batch);
            await _context.SaveChangesAsync();
        }

        var shelvingAction = new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = item.Quantity,
            Type = dto.AddToShelf ? ShelvingActionType.AddedToShelf : ShelvingActionType.AddedToStorage,
            ActionAt = DateTime.UtcNow,
            InvoiceItemId = item.Id
        };

        _context.ShelvingActions.Add(shelvingAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added invoice item {ItemId} to stock: Product {ProductId}, Quantity {Quantity}, Type {Type}",
            itemId, productId, item.Quantity, shelvingAction.Type);

        var result = new ShelvingActionDto
        {
            Id = shelvingAction.Id,
            ProductBatchId = batch.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            ProductBarcode = dto.ProductBarcode ?? string.Empty,
            BestBeforeDate = batch.BestBeforeDate,
            Quantity = shelvingAction.Quantity,
            Type = shelvingAction.Type,
            ActionAt = shelvingAction.ActionAt,
            InvoiceItemId = shelvingAction.InvoiceItemId
        };

        return Ok(result);
    }
}
