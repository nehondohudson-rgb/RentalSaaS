using System.ComponentModel.DataAnnotations;

namespace RentalSaaS.Api.Models;

public class ApplicationUser
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "PropertyManager";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PropertyManagerAssignment> PropertyAssignments { get; set; } = new();
}
