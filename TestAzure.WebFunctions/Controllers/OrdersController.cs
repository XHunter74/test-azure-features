using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using TestAzure.Shared;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;
using TestAzure.WebFunctions.Exceptions;
using TestAzure.WebFunctions.Services;

namespace TestAzure.AcceptingOrders.Controllers;

public class OrdersController(ILogger<OrdersController> logger,
    OrdersService _ordersService, ServiceBusService serviceBusService) : BaseFunctions(logger, serviceBusService)
{
    [Function("NewOrder")]
    public async Task<HttpResponseData> NewOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating a new order");
        NewOrderDto? newOrder;
        try
        {
            newOrder = await req.ReadFromJsonAsync<NewOrderDto>(cancellationToken);
        }
        catch
        {
            throw new BadRequestException("Invalid request payload");
        }

        //TODO : Validate the newOrder object here. Check if CustomerName, ProductName and Quantity are not null or empty.
        if (newOrder == null)
        {
            throw new BadRequestException("Invalid request payload");
        }

        var placedOrder = await _ordersService.CreateOrderAsync(newOrder, cancellationToken);

        await ServiceBusService.SendMessageToServiceBus(Constants.NewOrdersQueue, placedOrder, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.Created);
        var location = new Uri(req.Url, $"orders/{placedOrder.OrderId}");
        response.Headers.Add("Location", location.ToString());
        await response.WriteAsJsonAsync(placedOrder, cancellationToken);
        return response;
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
            throw new BadRequestException("Invalid request payload");
        }

        var order = await _ordersService.GetOrderByIdAsync(guid, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(order, cancellationToken);
        return response;
    }
}