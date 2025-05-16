using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TestAzure.Shared;
using TestAzure.Shared.Models;

namespace TestAzure.ProcessOrders;

public class ProcessOrderMethods(ILogger<ProcessOrderMethods> logger) : BaseFunctions(logger)
{

    [Function(nameof(ProcessNewOrder))]
    public async Task ProcessNewOrder(
        [ServiceBusTrigger(Constants.NewOrdersQueue, Connection = "ServiceBusConnection")]
                    ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        Logger.LogInformation("Message ID: {id}", message.MessageId);
        Logger.LogInformation("Message Body: {body}", message.Body);
        Logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var order = JsonSerializer.Deserialize<PlacedOrderDto>(message.Body.ToString());

        if (order.Status != (int)OrderStatus.New)
        {
            Logger.LogWarning("Order is not new. Sendig to Dead Letter Queue.");
            await messageActions.DeadLetterMessageAsync(
                message,
                null,
                deadLetterReason: "ProcessingError",
                deadLetterErrorDescription: "An error occurred while processing the message."
            );
            return;
        }



        await messageActions.CompleteMessageAsync(message);
    }
}