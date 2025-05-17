using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using TestAzure.Shared;
using TestAzure.Shared.Models.Dto;

namespace TestAzure.ProcessOrders;

public class ProcessOrderMethods(ILogger<ProcessOrderMethods> logger) : BaseFunctions(logger)
{
    [Function("ProcessDeadLetters")]
    public async Task ProcessDeadLetters(
    [ServiceBusTrigger(Constants.NewOrdersQueue+"/$DeadLetterQueue", Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
    ServiceBusMessageActions messageActions, CancellationToken cancellationToken)
    {
        var reason = message.DeadLetterReason;
        var description = message.DeadLetterErrorDescription;
        try
        {
            var errorsClient = new TableClient(StorageConnectionString, "ordererrors");
            var deadOrder = JsonSerializer.Deserialize<PlacedOrderDto>(message.Body.ToString());
            var errorEntity = new TableEntity("errors", deadOrder.OrderId.ToString())
            {
                ["SerializedOrder"] = message.Body.ToString(),
                ["Reason"] = reason ?? string.Empty,
                ["Description"] = description ?? string.Empty,
                ["LoggedAt"] = DateTime.UtcNow
            };
            await errorsClient.AddEntityAsync(errorEntity, cancellationToken);
            var ordersClient = new TableClient(StorageConnectionString, "orders");
            var updateEntity = new TableEntity("orders", deadOrder.OrderId.ToString())
            {
                ["Status"] = (int)OrderStatus.Error
            };
            await ordersClient.UpdateEntityAsync(updateEntity, ETag.All, TableUpdateMode.Merge, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record dead-lettered order.");
        }

        await messageActions.CompleteMessageAsync(message);
    }

    [Function(nameof(ProcessNewOrder))]
    public async Task ProcessNewOrder(
        [ServiceBusTrigger(Constants.NewOrdersQueue, Connection = "ServiceBusConnection")]
                    ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Message ID: {id}", message.MessageId);
        Logger.LogInformation("Message Body: {body}", message.Body);
        Logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        //TODO Need to add validation of the message and order.
        var order = JsonSerializer.Deserialize<PlacedOrderDto>(message.Body.ToString());

        if (order.Status != (int)OrderStatus.New)
        {
            Logger.LogWarning("Order is not new. Sending to Dead Letter Queue.");
            await messageActions.DeadLetterMessageAsync(
                message,
                null,
                deadLetterReason: "Incorrect order status",
                deadLetterErrorDescription: $"Incorrect order status: {order.Status.ToString()}"
            );
            return;
        }

        if (order.Quantity == 5) // For testing purposes only
        {
            Logger.LogWarning("Order is not new. Sending to Dead Letter Queue.");
            await messageActions.DeadLetterMessageAsync(
                message,
                null,
                deadLetterReason: "Incorrect order quantity",
                deadLetterErrorDescription: $"Incorrect order quantity: {order.Quantity}"
            );
            return;
        }

        try
        {

            var ordersClient = new TableClient(StorageConnectionString, "orders");
            var updateEntity = new TableEntity("orders", order.OrderId.ToString())
            {
                ["Status"] = (int)OrderStatus.Completed
            };
            await ordersClient.UpdateEntityAsync(updateEntity, ETag.All, TableUpdateMode.Merge, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while updating the order status in Azure Table Storage.");
        }
        await SendMessageToServiceBus(Constants.ReportsQueue, order, cancellationToken);

        await messageActions.CompleteMessageAsync(message, cancellationToken);
    }
}