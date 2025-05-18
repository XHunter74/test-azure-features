using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestAzure.Shared.Services;
using TestAzure.WebFunctions.Midddlewares;
using TestAzure.WebFunctions.Services;

var builder = Host.CreateDefaultBuilder(args); // Changed to Host.CreateDefaultBuilder

builder.ConfigureFunctionsWorkerDefaults(worker =>
{
    worker.UseMiddleware<GlobalExceptionMiddleware>();
});

builder.ConfigureServices(services =>
{
    services.AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

    services.AddScoped<ItemsService>();
    services.AddScoped<OrdersService>();
    services.AddScoped<ServiceBusService>();
});

var host = builder.Build();
host.Run();
