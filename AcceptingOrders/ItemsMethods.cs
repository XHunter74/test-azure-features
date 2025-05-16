using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TestAzure.Shared.Models;

namespace TestAzure.AcceptingOrders
{
    public class ItemsMethods
    {
        private readonly ILogger<ItemsMethods> _logger;
        public ItemsMethods(ILogger<ItemsMethods> logger) => _logger = logger;

        [Function("GetItems")]
        public async Task<HttpResponseData> GetItems(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items")] HttpRequestData req, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching items from Azure Table Storage [Items]");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(connectionString, "items");
            var items = new List<ItemDto>();

            await foreach (var entity in tableClient.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
            {
                items.Add(new ItemDto
                {
                    Id = Guid.Parse(entity.RowKey),
                    Name = entity.GetString("Name") ?? string.Empty,
                    Price = Convert.ToDecimal(entity.GetDouble("Price"))
                });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items, cancellationToken);
            return response;
        }

        [Function("NewItem")]
        public async Task<HttpResponseData> AddNewItem(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "items")] HttpRequestData req, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received request to add a new item.");

            NewItemDto? newItemDto = null;

            try
            {
                newItemDto = await req.ReadFromJsonAsync<NewItemDto>(cancellationToken);
            }
            catch (Exception ex)
            {
                return await CreateBadRequestResponse(req);
            }
            if (newItemDto == null || (string.IsNullOrEmpty(newItemDto.Name) || newItemDto.Price <= 0))
            {
                return await CreateBadRequestResponse(req);
            }
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(connectionString, "items");
            var id = Guid.NewGuid();
            var entity = new TableEntity("items", id.ToString())
            {
                ["Name"] = newItemDto.Name,
                ["Price"] = Convert.ToDouble(newItemDto.Price)
            };

            _logger.LogInformation("Adding new item with ID {ItemId} to Azure Table Storage.", id);

            await tableClient.AddEntityAsync(entity, cancellationToken);

            var itemDto = new ItemDto { Id = id, Name = newItemDto.Name, Price = newItemDto.Price };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(itemDto, cancellationToken);

            _logger.LogInformation("Successfully added new item with ID {ItemId}.", id);

            return response;
        }

        private async Task<HttpResponseData> CreateBadRequestResponse(HttpRequestData req)
        {
            _logger.LogWarning("Invalid request payload received.");
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync("Invalid request payload");
            return response;
        }
    }
}