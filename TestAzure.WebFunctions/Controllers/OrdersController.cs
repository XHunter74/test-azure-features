using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using TestAzure.AcceptingOrders.Services;
using TestAzure.Shared;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;

namespace TestAzure.AcceptingOrders.Controllers;

public class OrdersController(ILogger<OrdersController> logger,
    OrdersService _ordersService, ServiceBusService serviceBusService) : BaseFunctions(logger, serviceBusService)
{
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

            var placedOrder = await _ordersService.CreateOrderAsync(newOrder, cancellationToken);

            if (placedOrder == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.BadRequest);
                await notFound.WriteStringAsync("Product not found", cancellationToken);
                return notFound;
            }

            await ServiceBusService.SendMessageToServiceBus(Constants.NewOrdersQueue, placedOrder, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.Created);
            var location = new Uri(req.Url, $"orders/{placedOrder.OrderId}");
            response.Headers.Add("Location", location.ToString());
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
    
    [Function("GetOrderById")]
    public async Task<HttpResponseData> GetOrderById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Retrieving order by ID {OrderId}", orderId);
        if (!Guid.TryParse(orderId, out var guid))
        {
            return await CreateBadRequestResponse(req);
        }

        var order = await _ordersService.GetOrderByIdAsync(guid, cancellationToken);
        if (order == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Order not found", cancellationToken);
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(order, cancellationToken);
        return response;
    }
}