using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalSaaS.Api.Data;
using RentalSaaS.Api.Models;

namespace RentalSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TenantsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants(
        [FromQuery] string? search,
        [FromQuery] bool activeOnly = false)
    {
        var query = _context.Tenants.AsNoTracking();

        if (activeOnly)
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                t.FullName.Contains(term) ||
                t.PhoneNumber.Contains(term) ||
                (t.Email != null && t.Email.Contains(term)) ||
                (t.NationalId != null && t.NationalId.Contains(term)));
        }

        return Ok(await query.OrderBy(t => t.FullName).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tenant>> GetTenant(int id)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return tenant is null
            ? NotFound(new { message = "Tenant not found." })
            : Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<Tenant>> CreateTenant(Tenant tenant)
    {
        tenant.Id = 0;
        tenant.FullName = tenant.FullName.Trim();
        tenant.PhoneNumber = tenant.PhoneNumber.Trim();
        tenant.Email = CleanOptional(tenant.Email);
        tenant.NationalId = CleanOptional(tenant.NationalId)?.ToUpperInvariant();
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.IsActive = true;

        if (await NationalIdExists(tenant.NationalId))
            return Conflict(new { message = "A tenant with this National ID already exists." });

        _context.Tenants.Add(tenant);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The tenant could not be saved. Check for duplicate National IDs." });
        }

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTenant(int id, Tenant updated)
    {
        var tenant = await _context.Tenants.FindAsync(id);

        if (tenant is null)
            return NotFound(new { message = "Tenant not found." });

        var nationalId = CleanOptional(updated.NationalId)?.ToUpperInvariant();

        if (await NationalIdExists(nationalId, id))
            return Conflict(new { message = "Another tenant already uses this National ID." });

        tenant.FullName = updated.FullName.Trim();
        tenant.PhoneNumber = updated.PhoneNumber.Trim();
        tenant.Email = CleanOptional(updated.Email);
        tenant.NationalId = nationalId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The tenant could not be updated. Check for duplicate National IDs." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeactivateTenant(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);

        if (tenant is null)
            return NotFound(new { message = "Tenant not found." });

        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> NationalIdExists(string? nationalId, int? exceptId = null)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return false;

        return await _context.Tenants.AnyAsync(t =>
            t.NationalId == nationalId &&
            (!exceptId.HasValue || t.Id != exceptId.Value));
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
