using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;
using TestAzure.WebFunctions.Exceptions;

namespace TestAzure.WebFunctions.Services;

public class OrdersService(ILogger<ItemsService> logger) : BaseService(logger)
{
    public async Task<PlacedOrderDto> CreateOrderAsync(NewOrderDto newOrder, CancellationToken cancellationToken = default)
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
            throw new NotFoundException($"Item with name {newOrder.ProductName} not found.");

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

    public async Task<PlacedOrderWithError?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var ordersClient = new TableClient(StorageConnectionString, "orders");
        var response = await ordersClient.GetEntityIfExistsAsync<TableEntity>(
            partitionKey: "orders",
            rowKey: orderId.ToString(),
            cancellationToken: cancellationToken);

        if (!response.HasValue)
            throw new NotFoundException($"Order with ID {orderId} not found.");

        var entity = response.Value;

        var placedOrder = new PlacedOrderWithError
        {
            OrderId = orderId,
            CustomerName = entity.GetString("CustomerName")!,
            ProductName = entity.GetString("ProductName")!,
            Quantity = entity.GetInt32("Quantity") ?? 0,
            UnitPrice = Convert.ToDecimal(entity.GetDouble("UnitPrice")),
            TotalPrice = Convert.ToDecimal(entity.GetDouble("TotalPrice")),
            Status = (OrderStatus)(entity.GetInt32("Status") ?? 0),
            PlacedAt = entity.GetDateTime("PlacedAt") ?? DateTime.MinValue
        };

        if (placedOrder.Status == OrderStatus.Error)
        {
            var errorsClient = new TableClient(StorageConnectionString, "ordererrors");
            var errorResponse = await errorsClient.GetEntityIfExistsAsync<TableEntity>(
                partitionKey: "errors",
                rowKey: orderId.ToString(),
                cancellationToken: cancellationToken);
            if (errorResponse.HasValue)
            {
                var errorEntity = errorResponse.Value;

                var errorDto = new OrderErrorDto
                {
                    Reason = errorEntity.GetString("Reason")!,
                    Description = errorEntity.GetString("Description")!
                };
                placedOrder.Error = errorDto;
            }
        }

        Logger.LogInformation("Order {OrderId} retrieved", orderId);
        return placedOrder;
    }
}
