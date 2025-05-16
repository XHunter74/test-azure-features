using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TestAzure.AcceptingOrders;

public class OrdersMethods
{
    private readonly ILogger<OrdersMethods> _logger;
    public OrdersMethods(ILogger<OrdersMethods> logger) => _logger = logger;

    [Function("OrdersMethods")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}