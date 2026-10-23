using System.ComponentModel.DataAnnotations;

namespace RentalSaaS.Api.Models;

public class Lease
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required]
    public int RentalUnitId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal MonthlyRent { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }

    public RentalUnit? RentalUnit { get; set; }
}
