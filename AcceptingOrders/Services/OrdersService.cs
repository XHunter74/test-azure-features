using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;

namespace TestAzure.AcceptingOrders.Services;

public class OrdersService(ILogger<ItemsService> logger) : BaseService(logger)
{
    public async Task<PlacedOrderDto?> CreateOrderAsync(NewOrderDto newOrder, CancellationToken cancellationToken = default)
    {
        var itemsClient = new TableClient(StorageConnectionString, "items");
        TableEntity? itemEntity = null;
        await foreach (var entity in itemsClient.QueryAsync<TableEntity>(
            filter: $"NormalizedName eq '{newOrder.ProductName.ToLowerInvariant()}'",
            cancellationToken: cancellationToken))
        {
            itemEntity = entity;
            break;
        }

        if (itemEntity == null)
            return null;
        
        var unitPrice = Convert.ToDecimal(itemEntity.GetDouble("Price"));
        var totalPrice = unitPrice * newOrder.Quantity;

        var orderId = Guid.NewGuid();
        var ordersClient = new TableClient(StorageConnectionString, "orders");
        var orderEntity = new TableEntity("orders", orderId.ToString())
        {
            ["CustomerName"] = newOrder.CustomerName,
            ["ProductName"] = newOrder.ProductName,
            ["Quantity"] = newOrder.Quantity,
            ["UnitPrice"] = Convert.ToDouble(unitPrice),
            ["TotalPrice"] = Convert.ToDouble(totalPrice),
            ["Status"] = (int)OrderStatus.New,
            ["PlacedAt"] = DateTime.UtcNow
        };
        await ordersClient.AddEntityAsync(orderEntity, cancellationToken);

        var placedOrder = new PlacedOrderDto
        {
            OrderId = orderId,
            CustomerName = newOrder.CustomerName,
            ProductName = newOrder.ProductName,
            Status = OrderStatus.New,
            Quantity = newOrder.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            PlacedAt = DateTime.UtcNow
        };

        Logger.LogInformation("Order created successfully with ID {OrderId}", orderId);

        return placedOrder;
    }
}
