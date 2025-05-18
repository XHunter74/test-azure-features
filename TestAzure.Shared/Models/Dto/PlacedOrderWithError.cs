namespace TestAzure.Shared.Models.Dto;

public class PlacedOrderWithError:PlacedOrderDto
{
    public OrderErrorDto Error { get; set; }
}
