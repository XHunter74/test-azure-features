using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAzure.DurableFunctions.Activities;
using TestAzure.DurableFunctions.Models;

namespace TestAzure.DurableFunctions.Orchestrators;

public class CalculateOrchestrator
{
    private const int PortionSize = 10;

    [FunctionName(nameof(CalculateOrchestrator))]
    public static async Task<FactorialResultDto> Run(
       [OrchestrationTrigger] IDurableOrchestrationContext context,
       ILogger log)
    {
        var inputValue = context.GetInput<int>();
        if (inputValue < 1)
        {
            throw new ArgumentException("Input value must be greater than 1.");
        }
        var startValue = 1;
        long factorial = 1;
        do
        {
            var endValue = startValue + PortionSize - 1;
            if (endValue > inputValue)
            {
                endValue = inputValue;
            }
            var model = new CalcModel
            {
                StartFactorial = factorial,
                StartValue = startValue,
                EndValue = endValue
            };
            factorial = await context.CallActivityAsync<long>(nameof(FactorialActivity), model);
            startValue = endValue + 1;

        } while (startValue < inputValue);



        var result = new FactorialResultDto
        {
            Value = inputValue,
            Result = factorial
        };

        return result;
    }
}
