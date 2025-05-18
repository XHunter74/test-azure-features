using Microsoft.Extensions.Logging;
using System.Text.Json;
using TestAzure.Shared.Models.Dto;

namespace TestAzure.QueueFunctions.Services;

public class SmsNotificationService(ILogger<SmsNotificationService> logger) : BaseNotificationService(logger), INotificationService
{
    private const string BlobContainerName = "sms";
    public NotificationType NotificationType => NotificationType.sms;

    public async Task SendNotificationAsync(PlacedOrderWithError order, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(order);
        await StoreContentToBlob(payload, BlobContainerName, cancellationToken);
    }
}
