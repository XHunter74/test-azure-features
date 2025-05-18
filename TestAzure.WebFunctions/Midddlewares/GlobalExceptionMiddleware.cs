using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;
using TestAzure.WebFunctions.Exceptions;

namespace TestAzure.WebFunctions.Midddlewares;

public sealed class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _log;
    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> log) => _log = log;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            _log.LogError(ex, "AppException caught: {Message}", ex.Message);
            var result = new AppExceptionModel
            {
                Type = ex.GetType().Name,
                Message = ex.Message
            };
            var httpReqData = await context.GetHttpRequestDataAsync();

            if (httpReqData != null)
            {
                var newHttpResponse = httpReqData.CreateResponse((HttpStatusCode)ex.HttpStatusCode);
                await newHttpResponse.WriteAsJsonAsync(result);

                var invocationResult = context.GetInvocationResult();

                invocationResult.Value = newHttpResponse;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception caught: {Message}", ex.Message);
            var result = new AppExceptionModel
            {
                Type = ex.GetType().Name,
                Message = $"An unexpected error occurred: {ex.Message}"
            };
            var httpReqData = await context.GetHttpRequestDataAsync();
            if (httpReqData != null)
            {
                var newHttpResponse = httpReqData.CreateResponse(HttpStatusCode.InternalServerError);
                await newHttpResponse.WriteAsJsonAsync(result);
                var invocationResult = context.GetInvocationResult();
                invocationResult.Value = newHttpResponse;
            }
        }
    }
}
