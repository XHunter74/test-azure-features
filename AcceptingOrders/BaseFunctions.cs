using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace TestAzure.AcceptingOrders;

public class BaseFunctions
{
    public ILogger Logger { get; set; }
    public BaseFunctions(ILogger logger) => Logger = logger;
    public static string StorageConnectionString => Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
    public static string ServiceBusConnectionString => Environment.GetEnvironmentVariable("ServiceBusConnectionString") ?? string.Empty;

    public async Task<HttpResponseData> CreateBadRequestResponse(HttpRequestData req)
    {
        Logger.LogWarning("Invalid request payload received.");
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteStringAsync("Invalid request payload");
        return response;
    }
}
