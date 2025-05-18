using TestAzure.Shared.Models.Dto;

namespace TestAzure.QueueFunctions.Services;

public interface INotificationService
{
    public NotificationType NotificationType { get; }
    public Task SendNotificationAsync(PlacedOrderWithError order, CancellationToken cancellationToken = default);
}
