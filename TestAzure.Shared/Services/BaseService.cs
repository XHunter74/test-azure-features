using Microsoft.Extensions.Logging;

namespace TestAzure.Shared.Services;

public class BaseService
{
    public ILogger Logger { get; set; }
    public BaseService(ILogger logger) => Logger = logger;
    public static string StorageConnectionString => Environment.GetEnvironmentVariable(Constants.StorageConnectionStringName) ?? string.Empty;
    public static string ServiceBusConnectionString => Environment.GetEnvironmentVariable(Constants.ServiceBusConnectionStringName) ?? string.Empty;
}
