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

    public InvoicesController(
        ApplicationDbContext context, 
        ILogger<InvoicesController> logger,
        InvoiceParserFactory parserFactory)
    {
        _context = context;
        _logger = logger;
        _parserFactory = parserFactory;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
    {
        var invoices = await _context.Invoices
            .Include(i => i.CreatedBy)
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
            InvoiceDate = dto.InvoiceDate,
            Supplier = dto.Supplier,
            AdditionalCosts = dto.AdditionalCosts,
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
                BestBeforeDate = itemDto.BestBeforeDate,
                Notes = itemDto.Notes,
                ArticleNumber = itemDto.ArticleNumber
            };
            invoice.Items.Add(item);
        }

        // Calculate total amount
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice) + invoice.AdditionalCosts;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created: {InvoiceNumber} with {ItemCount} items", 
            invoice.InvoiceNumber, invoice.Items.Count);

        // Load the created by user for the response
        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();

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
    public ActionResult<ParseInvoiceResponse> ParseInvoice([FromBody] ParseInvoiceRequest request)
    {
        var parser = _parserFactory.GetParser(request.Format);
        if (parser == null)
        {
            return BadRequest(new ParseInvoiceResponse
            {
                Success = false,
                ErrorMessage = $"Unsupported invoice format: {request.Format}. Supported formats: {string.Join(", ", _parserFactory.GetSupportedFormats())}"
            });
        }

        var result = parser.Parse(request.InvoiceText);
        
        _logger.LogInformation("Invoice parsed: Format={Format}, Success={Success}, Items={ItemCount}", 
            request.Format, result.Success, result.Items.Count);

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
            InvoiceDate = dto.InvoiceDate,
            Supplier = dto.Supplier,
            AdditionalCosts = dto.AdditionalCosts,
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
                BestBeforeDate = itemDto.BestBeforeDate,
                ArticleNumber = itemDto.ArticleNumber
            };
            invoice.Items.Add(item);
        }

        // Calculate total amount
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice) + invoice.AdditionalCosts;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created from parsed data: {InvoiceNumber} with {ItemCount} items", 
            invoice.InvoiceNumber, invoice.Items.Count);

        // Load the created by user for the response
        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();

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

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, result);
    }

    [HttpGet("formats")]
    public ActionResult<IEnumerable<string>> GetSupportedFormats()
    {
        return Ok(_parserFactory.GetSupportedFormats());
    }
}
