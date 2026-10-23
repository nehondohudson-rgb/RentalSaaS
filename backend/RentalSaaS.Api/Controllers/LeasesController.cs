using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalSaaS.Api.Data;
using RentalSaaS.Api.Models;

namespace RentalSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LeasesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Lease>>> GetLeases(
        [FromQuery] string? status)
    {
        var query = _context.Leases
            .AsNoTracking()
            .Include(l => l.Tenant)
            .Include(l => l.RentalUnit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var requestedStatus = status.Trim();

            query = query.Where(l => l.Status == requestedStatus);
        }

        var leases = await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(leases);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lease>> GetLease(int id)
    {
        var lease = await _context.Leases
            .AsNoTracking()
            .Include(l => l.Tenant)
            .Include(l => l.RentalUnit)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lease is null)
            return NotFound(new { message = "Lease not found." });

        return Ok(lease);
    }

    [HttpPost]
    public async Task<ActionResult<Lease>> CreateLease(
        CreateLeaseRequest request)
    {
        if (request.TenantId <= 0 || request.RentalUnitId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid TenantId and RentalUnitId are required."
            });
        }

        if (request.StartDate == default)
        {
            return BadRequest(new
            {
                message = "A valid lease StartDate is required."
            });
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t =>
                t.Id == request.TenantId && t.IsActive);

        if (tenant is null)
        {
            return BadRequest(new
            {
                message = "The tenant does not exist or is inactive."
            });
        }

        var unit = await _context.RentalUnits
            .FirstOrDefaultAsync(u => u.Id == request.RentalUnitId);

        if (unit is null)
        {
            return NotFound(new { message = "Rental unit not found." });
        }

        if (!string.Equals(unit.Status, "Vacant",
            StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                message = "The unit is not vacant and cannot be allocated."
            });
        }

        var hasActiveLease = await _context.Leases.AnyAsync(l =>
            l.RentalUnitId == unit.Id && l.Status == "Active");

        if (hasActiveLease)
        {
            return Conflict(new
            {
                message = "This unit already has an active lease."
            });
        }

        var agreedRent = request.MonthlyRent ?? unit.MonthlyRent;

        if (agreedRent <= 0)
        {
            return BadRequest(new
            {
                message = "Monthly rent must be greater than zero."
            });
        }

        var lease = new Lease
        {
            TenantId = tenant.Id,
            RentalUnitId = unit.Id,
            StartDate = request.StartDate,
            MonthlyRent = agreedRent,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        unit.Status = "Occupied";
        _context.Leases.Add(lease);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The lease could not be saved. The unit may already have an active lease."
            });
        }

        await _context.Entry(lease).Reference(l => l.Tenant).LoadAsync();
        await _context.Entry(lease).Reference(l => l.RentalUnit).LoadAsync();

        return CreatedAtAction(
            nameof(GetLease),
            new { id = lease.Id },
            lease);
    }

    [HttpPost("{id:int}/end")]
    public async Task<IActionResult> EndLease(int id)
    {
        var lease = await _context.Leases
            .Include(l => l.RentalUnit)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lease is null)
            return NotFound(new { message = "Lease not found." });

        if (lease.Status != "Active")
        {
            return Conflict(new
            {
                message = "This lease is already ended."
            });
        }

        lease.Status = "Ended";
        lease.EndDate = DateTime.UtcNow;

        if (lease.RentalUnit is not null)
        {
            lease.RentalUnit.Status = "Vacant";
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Lease ended successfully. The unit is now vacant.",
            leaseId = lease.Id,
            lease.Status,
            lease.EndDate
        });
    }
}

public class CreateLeaseRequest
{
    public int TenantId { get; set; }

    public int RentalUnitId { get; set; }

    public DateTime StartDate { get; set; }

    public decimal? MonthlyRent { get; set; }
}
