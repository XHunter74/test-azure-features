namespace TestAzure.Shared.Models.Dto;

public class NewOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
