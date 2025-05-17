namespace TestAzure.Shared.Models.Dto;

public enum OrderStatus
{
    New = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3,
    Shipped = 4,
    Delivered = 5,
    Returned = 6,
    Refunded = 7,
    Error = 100,
}
