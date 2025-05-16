namespace TestAzure.Shared.Models;

public class PlacedOrderDto:NewOrderDto
{
    public Guid OrderId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}
