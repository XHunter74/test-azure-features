using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TestAzure.Shared.Services;

public class ServiceBusService(ILogger<ServiceBusService> logger) : BaseService(logger)
{
    public static async Task SendMessageToServiceBus(string queueName, object message,
        CancellationToken cancellationToken, bool createQueue = true)
    {
        if (createQueue)
        {
            var adminClient = new ServiceBusAdministrationClient(ServiceBusConnectionString);
            if (!await adminClient.QueueExistsAsync(queueName, cancellationToken))
            {
                await adminClient.CreateQueueAsync(queueName, cancellationToken: cancellationToken);
            }
        }
        await using var serviceBusClient = new ServiceBusClient(ServiceBusConnectionString);
        var ordersQueueSender = serviceBusClient.CreateSender(queueName);
        var orderMessage = new ServiceBusMessage(JsonSerializer.Serialize(message));
        await ordersQueueSender.SendMessageAsync(orderMessage, cancellationToken);
    }
}
