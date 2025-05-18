using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TestAzure.DurableFunctions.Models;

namespace TestAzure.DurableFunctions.Activities;

public class FactorialActivity
{
    [FunctionName(nameof(FactorialActivity))]
    public static long Run(
        [ActivityTrigger] CalcModel model,
        ILogger logger)
    {
        var value = model.StartFactorial;
        logger.LogInformation($"Calculating factorial for {model.StartValue}");
        for (int i = model.StartValue; i <= model.EndValue; i++)
        {
            value *= i;
            logger.LogInformation($"Current factorial value: {model.StartFactorial}");
        }
        return value;
    }
}
