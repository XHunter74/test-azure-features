using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using TestAzure.Shared.Models;
using TestAzure.Shared;

namespace TestAzure.AcceptingOrders;

public class OrdersMethods : BaseFunctions
{
    public OrdersMethods(ILogger<OrdersMethods> logger) : base(logger) { }

    [Function("NewOrder")]
    public async Task<HttpResponseData> NewOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Creating a new order");
            NewOrderDto? newOrder = null;
            try
            {
                newOrder = await req.ReadFromJsonAsync<NewOrderDto>(cancellationToken);
            }
            catch
            {
                return await CreateBadRequestResponse(req);
            }
            //TODO : Validate the newOrder object here. Check if CustomerName, ProductName and Quantity are not null or empty.
            if (newOrder == null)
            {
                return await CreateBadRequestResponse(req);
            }

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
            {
                var notFound = req.CreateResponse(HttpStatusCode.BadRequest);
                await notFound.WriteStringAsync("Product not found", cancellationToken);
                return notFound;
            }
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

            await SendMessageToServiceBus(Constants.NewOrdersQueue, placedOrder, cancellationToken);
            
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(placedOrder, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while processing the order.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing the order.", cancellationToken);
            return errorResponse;
        }
    }
}