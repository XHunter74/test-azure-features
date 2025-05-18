using Microsoft.Extensions.Logging;
using System.Text.Json;
using TestAzure.Shared.Models.Dto;

namespace TestAzure.QueueFunctions.Services;

public class EmailNotificationService(ILogger<EmailNotificationService> logger) : BaseNotificationService(logger), INotificationService
{
    private const string BlobContainerName = "email";
    public NotificationType NotificationType => NotificationType.email;

    public async Task SendNotificationAsync(PlacedOrderWithError order, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(order);
        await StoreContentToBlob(payload, BlobContainerName, cancellationToken);
    }
}
