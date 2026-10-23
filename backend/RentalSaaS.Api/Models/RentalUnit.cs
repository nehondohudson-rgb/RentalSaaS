using System.Text.Json.Serialization;

namespace RentalSaaS.Api.Models;

public class RentalUnit
{
    public int Id { get; set; }

    public string UnitNumber { get; set; } = string.Empty;

    public decimal MonthlyRent { get; set; }

    public string Status { get; set; } = "Vacant";

    public int PropertyId { get; set; }

    [JsonIgnore]
    public Property? Property { get; set; }
}
