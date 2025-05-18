using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;

namespace TestAzure.WebFunctions.Services;

public class ItemsService(ILogger<ItemsService> logger) : BaseService(logger)
{
    public async Task<List<ItemDto>> GetAllItemsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching items from Azure Table Storage [Items]");
        var tableClient = new TableClient(StorageConnectionString, "items");
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
        return items;
    }

    public async Task<ItemDto> CreateNewItemAsync(NewItemDto newItemDto, CancellationToken cancellationToken = default)
    {
        var tableClient = new TableClient(StorageConnectionString, "items");
        var id = Guid.NewGuid();
        var entity = new TableEntity("items", id.ToString())
        {
            ["Name"] = newItemDto.Name,
            ["Price"] = Convert.ToDouble(newItemDto.Price),
            ["NormalizedName"] = newItemDto.Name.ToLowerInvariant()
        };

        Logger.LogInformation("Adding new item with ID {ItemId} to Azure Table Storage.", id);

        await tableClient.AddEntityAsync(entity, cancellationToken);

        var itemDto = new ItemDto { Id = id, Name = newItemDto.Name, Price = newItemDto.Price };
        Logger.LogInformation("Successfully added new item with ID {ItemId}.", id);

        return itemDto;
    }
}
