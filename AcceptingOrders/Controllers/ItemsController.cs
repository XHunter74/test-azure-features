using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using TestAzure.AcceptingOrders.Services;
using TestAzure.Shared;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;

namespace TestAzure.AcceptingOrders.Controllers;

public class ItemsController(ILogger<ItemsController> logger,
    ItemsService _itemService, ServiceBusService serviceBusService) : BaseFunctions(logger, serviceBusService)
{
    [Function("GetItems")]
    public async Task<HttpResponseData> GetItems(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items")] HttpRequestData req, CancellationToken cancellationToken)
    {

        var items = await _itemService.GetAllItemsAsync(cancellationToken);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items, cancellationToken);
        return response;
    }

    [Function("NewItem")]
    public async Task<HttpResponseData> AddNewItem(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "items")] HttpRequestData req, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Received request to add a new item.");

        NewItemDto? newItemDto = null;

        try
        {
            newItemDto = await req.ReadFromJsonAsync<NewItemDto>(cancellationToken);
        }
        catch
        {
            return await CreateBadRequestResponse(req);
        }
        if (newItemDto == null || string.IsNullOrEmpty(newItemDto.Name) || newItemDto.Price <= 0)
        {
            return await CreateBadRequestResponse(req);
        }

        var itemDto = await _itemService.CreateNewItemAsync(newItemDto, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(itemDto, cancellationToken);

        return response;
    }
}