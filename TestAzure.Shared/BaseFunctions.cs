using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace TestAzure.Shared;

public class BaseFunctions
{
    public ILogger Logger { get; set; }
    public BaseFunctions(ILogger logger) => Logger = logger;
    public static string StorageConnectionString => Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
    public static string ServiceBusConnectionString => Environment.GetEnvironmentVariable("ServiceBusConnection") ?? string.Empty;

    public async Task<HttpResponseData> CreateBadRequestResponse(HttpRequestData req)
    {
        Logger.LogWarning("Invalid request payload received.");
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteStringAsync("Invalid request payload");
        return response;
    }

    public async Task SendMessageToServiceBus(string queueName, object message, CancellationToken cancellationToken)
    {
        var adminClient = new ServiceBusAdministrationClient(ServiceBusConnectionString);
        if (!await adminClient.QueueExistsAsync(queueName, cancellationToken))
        {
            await adminClient.CreateQueueAsync(queueName, cancellationToken: cancellationToken);
        }

        await using var serviceBusClient = new ServiceBusClient(ServiceBusConnectionString);
        var ordersQueueSender = serviceBusClient.CreateSender(queueName);
        var orderMessage = new ServiceBusMessage(JsonSerializer.Serialize(message));
        await ordersQueueSender.SendMessageAsync(orderMessage, cancellationToken);
    }
}
