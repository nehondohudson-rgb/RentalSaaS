namespace RentalSaaS.Api.Models;

public class Property
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string LogoDataUrl { get; set; } = string.Empty;

    public string PrimaryColor { get; set; } = "#245b45";

    public string AccentColor { get; set; } = "#eaf7ef";

    public List<RentalUnit> Units { get; set; } = new();
}
