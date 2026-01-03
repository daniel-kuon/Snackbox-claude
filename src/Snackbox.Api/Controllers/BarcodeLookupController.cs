using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BarcodeLookupController : ControllerBase
{
    private readonly IBarcodeLookupService _barcodeLookupService;

    public BarcodeLookupController(IBarcodeLookupService barcodeLookupService)
    {
        _barcodeLookupService = barcodeLookupService;
    }

    [HttpGet("{barcode}")]
    public async Task<ActionResult<BarcodeLookupResponseDto>> LookupBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return BadRequest(new { message = "Barcode is required" });
        }

        var result = await _barcodeLookupService.LookupBarcodeAsync(barcode);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result);
    }
}
