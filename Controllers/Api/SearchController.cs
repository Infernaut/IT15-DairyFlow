using IT15_DairyFlow.Data;
using IT15_DairyFlow.Security;
using IT15_DairyFlow.Security.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly ILookupHashService _lookup;

    public SearchController(ApplicationDbContext context, ITenantContext tenant, ILookupHashService lookup)
    {
        _context = context;
        _tenant = tenant;
        _lookup = lookup;
    }

    // Used by pages that have search boxes (PLM products, Sales, Transactions, etc.) to search encrypted-at-rest columns.
    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? q, CancellationToken ct)
    {
        var companyId = _tenant.CompanyId;
        if (companyId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());

        var h = _lookup.Compute(q);

        var items = await _context.Product
            .Where(p => p.CompanyID == companyId && p.ProductNameLookupHash == h)
            .OrderBy(p => p.ProductNameEncrypted)
            .Select(p => new { id = p.ProductID, name = p.ProductName ?? string.Empty })
            .Take(20)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("batches")]
    public async Task<IActionResult> Batches([FromQuery] string? q, CancellationToken ct)
    {
        var companyId = _tenant.CompanyId;
        if (companyId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());

        var h = _lookup.Compute(q);

        var items = await _context.ProductionBatch
            .Where(b => b.CompanyID == companyId && b.BatchCodeLookupHash == h)
            .OrderByDescending(b => b.ProductionBatchID)
            .Select(b => new { id = b.ProductionBatchID, code = b.BatchCode ?? string.Empty })
            .Take(20)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("equipment")]
    public async Task<IActionResult> Equipment([FromQuery] string? q, CancellationToken ct)
    {
        var companyId = _tenant.CompanyId;
        if (companyId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());

        var h = _lookup.Compute(q);

        var items = await _context.Equipment
            .Where(e => e.CompanyID == companyId && e.EquipmentNameLookupHash == h)
            .OrderBy(e => e.EquipmentNameEncrypted)
            .Select(e => new { id = e.EquipmentID, name = e.EquipmentName ?? string.Empty })
            .Take(20)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(items);
    }
}
