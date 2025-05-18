using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using TestAzure.Shared.Services;

namespace TestAzure.Shared;

public class BaseFunctions
{
    public ILogger Logger { get; set; }
    public ServiceBusService ServiceBusService { get; set; }

    public BaseFunctions(ILogger logger, ServiceBusService serviceBusService)
    {
        Logger = logger;
        ServiceBusService = serviceBusService;
    }

    public static string StorageConnectionString => Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
    
}
