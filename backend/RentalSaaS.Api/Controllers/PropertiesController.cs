using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalSaaS.Api.Data;
using RentalSaaS.Api.Models;

namespace RentalSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PropertiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Property>>> GetProperties()
    {
        var properties = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(properties);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Property>> GetProperty(int id)
    {
        var property = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
            return NotFound(new { message = "Property not found." });

        return Ok(property);
    }

    [HttpPost]
    public async Task<ActionResult<Property>> CreateProperty(Property property)
    {
        property.Id = 0;
        property.CreatedAt = DateTime.UtcNow;

        foreach (var unit in property.Units)
        {
            unit.Id = 0;
            unit.PropertyId = 0;
            unit.Property = null;
        }

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProperty),
            new { id = property.Id },
            property);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProperty(
        int id,
        Property updatedProperty)
    {
        var property = await _context.Properties.FindAsync(id);

        if (property is null)
            return NotFound(new { message = "Property not found." });

        if (string.IsNullOrWhiteSpace(updatedProperty.Name) ||
            string.IsNullOrWhiteSpace(updatedProperty.Address) ||
            string.IsNullOrWhiteSpace(updatedProperty.OwnerName))
        {
            return BadRequest(new
            {
                message = "Property name, address and owner are required."
            });
        }

        if (updatedProperty.LogoDataUrl.Length > 3000000)
        {
            return BadRequest(new
            {
                message = "Logo is too large. Choose an image under approximately 2 MB."
            });
        }

        property.Name = updatedProperty.Name.Trim();
        property.Address = updatedProperty.Address.Trim();
        property.OwnerName = updatedProperty.OwnerName.Trim();
        property.LogoDataUrl = updatedProperty.LogoDataUrl ?? string.Empty;
        property.PrimaryColor = updatedProperty.PrimaryColor;
        property.AccentColor = updatedProperty.AccentColor;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var property = await _context.Properties.FindAsync(id);

        if (property is null)
            return NotFound(new { message = "Property not found." });

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
