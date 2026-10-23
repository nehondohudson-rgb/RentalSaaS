namespace RentalSaaS.Api.Models;

public class PropertyManagerAssignment
{
    public int Id { get; set; }

    public int ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
