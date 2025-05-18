using Azure.Messaging.ServiceBus;
using Google.Protobuf.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TestAzure.ProcessOrders.Queues;
using TestAzure.QueueFunctions.Services;
using TestAzure.Shared;
using TestAzure.Shared.Models.Dto;
using TestAzure.Shared.Services;

namespace TestAzure.QueueFunctions.Subscriptions;

public class ManageSubscriptions(ILogger<ManageOrdersQueue> logger,
    ServiceBusService serviceBusService,
    IEnumerable<INotificationService> _notificationServices) : BaseFunctions(logger, serviceBusService)
{
    [Function(nameof(SendEmailAsync))]
    public async Task SendEmailAsync(
        [ServiceBusTrigger("order-created", "email", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        Logger.LogInformation("Message ID: {id}", message.MessageId);
        Logger.LogInformation("Message Body: {body}", message.Body);

        var order = await GetOrderAsync(message, messageActions);

        if (order == null)
        {
            Logger.LogError("Order is null after deserialization.");
            return;
        }

        var notificationService = _notificationServices.FirstOrDefault(x => x is EmailNotificationService);

        if (notificationService == null)
        {
            Logger.LogError("No notification service found for email.");
            return;
        }

        await notificationService.SendNotificationAsync(order, CancellationToken.None);


        await messageActions.CompleteMessageAsync(message);
    }

    [Function(nameof(SendSmsAsync))]
    public async Task SendSmsAsync(
        [ServiceBusTrigger("order-created", "sms", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        Logger.LogInformation("Message ID: {id}", message.MessageId);
        Logger.LogInformation("Message Body: {body}", message.Body);

        var order = await GetOrderAsync(message, messageActions);

        if (order == null)
        {
            Logger.LogError("Order is null after deserialization.");
            return;
        }

        var notificationService = _notificationServices.FirstOrDefault(x => x is SmsNotificationService);

        if (notificationService == null)
        {
            Logger.LogError("No notification service found for email.");
            return;
        }

        await notificationService.SendNotificationAsync(order, CancellationToken.None);


        await messageActions.CompleteMessageAsync(message);
    }

    private async Task<PlacedOrderWithError?> GetOrderAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        PlacedOrderWithError order = null;
        try
        {
            order = JsonSerializer.Deserialize<PlacedOrderWithError>(message.Body.ToString());
            return order;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deserialize message body.");
            await messageActions.DeadLetterMessageAsync(
                message,
                null,
                deadLetterReason: "Failed to deserialize message body.",
                deadLetterErrorDescription: "Failed to deserialize message body."
            );
            return order;
        }
    }
}