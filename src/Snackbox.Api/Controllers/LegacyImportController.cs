using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services.LegacyImport;

namespace Snackbox.Api.Controllers;

/// <summary>
/// Pulls the old Snackbox SQL Server database into this one and compares the two afterwards.
/// Admin only - the request carries the credentials of that database, and Program.cs keeps
/// this path out of the verbose HTTP body logging for the same reason.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class LegacyImportController : ControllerBase
{
    private readonly ILegacyImportService _service;

    public LegacyImportController(ILegacyImportService service)
    {
        _service = service;
    }

    /// <summary>Connects and reports what is in the old database, without changing anything.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<LegacyConnectionTestDto>> Test([FromBody] LegacyConnectionDto connection, CancellationToken ct)
        => Ok(await _service.TestAsync(connection, ct));

    /// <summary>
    /// Imports users, cards, scans (grouped into purchases) and payments. Safe to run again:
    /// rows that were already imported are recognised by their old primary key and skipped.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<LegacyImportResultDto>> Import([FromBody] LegacyImportRequestDto request, CancellationToken ct)
        => Ok(await _service.ImportAsync(request, ct));

    /// <summary>
    /// Compares both databases and reports every scan only one of them has. Use it after the
    /// import, and again during the parallel run to confirm nothing was missed.
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<LegacyVerificationDto>> Verify([FromBody] LegacyVerificationRequestDto request, CancellationToken ct)
        => Ok(await _service.VerifyAsync(request, ct));
}
