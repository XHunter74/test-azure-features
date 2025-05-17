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