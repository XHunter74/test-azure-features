using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TestAzure.DurableFunctions.Orchestrators;

namespace TestAzure.DurableFunctions.Controllers;

public static class CalculateController
{
    [FunctionName(nameof(Calculate))]
    public static async Task<IActionResult> Calculate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "calculate")] HttpRequest req,
        ILogger logger, [DurableClient] IDurableOrchestrationClient starter)
    {
        logger.LogInformation("Initiated durable process");

        string value = req.Query["value"];
        if (string.IsNullOrWhiteSpace(value))
        {
            return new BadRequestObjectResult("Missing required query parameter: value");
        }
        if (!int.TryParse(value, out int parsedValue))
        {
            return new BadRequestObjectResult("Invalid value parameter. Must be an integer.");
        }

        if (!(parsedValue>=1 && parsedValue <= 20))
        {
            return new BadRequestObjectResult("Value must be between 1 and 20.");
        }

        string instanceId = await starter.StartNewAsync(nameof(CalculateOrchestrator), input: value);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
