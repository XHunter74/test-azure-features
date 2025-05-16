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

        [Function("ItemsMethods")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items")] HttpRequestData req, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching items from Azure Table Storage [Items]");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(connectionString, "items");
            var items = new List<ItemDto>();

            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                items.Add(new ItemDto
                {
                    Id = Guid.Parse(entity.RowKey),
                    Name = entity.GetString("Name") ?? string.Empty,
                    Price = Convert.ToDecimal(entity.GetDouble("Price"))
                });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }
    }
}