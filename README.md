# TestAzureFeatures Solution

This repository contains a suite of Azure Functions and related projects demonstrating a complete Order Processing workflow using .NET 8 (isolated worker), Azure Table Storage, Service Bus, and Durable Functions.

## Projects

- **TestAzure.Shared**: Shared models, DTOs, services (base classes, ServiceBus helper, etc.).
 - **TestAzure.WebFunctions**: HTTP-triggered Web API for Items and Orders. It provides:
   - `GetItems` (GET `/items`): retrieve all items from Table Storage
   - `NewItem` (POST `/items`): add a new item to the items table
   - `NewOrder` (POST `/orders`): create a new order, store in the orders table, and enqueue to Service Bus
     returning a `Location` header referencing the new resource
   - `GetOrderById` (GET `/orders/{orderId}`): fetch a single order by ID, including any error details
   - Built on top of `OrdersService` and `ServiceBusService` for business logic, validation, and messaging
- **TestAzure.ProcessOrders**: Service Bus-triggered Functions:
  - `ProcessNewOrder`: listen on `orders-queue`, update order status to Completed in Table Storage, publish to `order-created` topic.
  - `ProcessDeadLetters`: handle dead-letter queue for `orders-queue`, record errors to Table Storage.
- **TestAzure.QueueFunctions**: Service Bus queue consumers and topic subscriptions:
  - **Queue handlers** (in `Queues/ManageOrdersQueue.cs`):
    - `ProcessDeadLetters` (ServiceBusTrigger on `orders-queue/$DeadLetterQueue`): logs DLQ messages, records errors to the `ordererrors` table, and marks orders as Error.
    - `ProcessNewOrder` (ServiceBusTrigger on `orders-queue`): updates order status to Completed in the `orders` table and publishes to the `order-created` topic.
  - **Topic subscriptions** (in `Subscriptions/ManageSubscriptions.cs`):
    - `SendEmailAsync` (subscription `email`): uses `EmailNotificationService` to serialize the `PlacedOrderWithError` and store it in Blob storage (container `email`).
    - `SendSmsAsync` (subscription `sms`): invokes an `SmsNotificationService` to send SMS notifications (implement as needed).
  - **Services** (in `Services/`): implementations of `INotificationService` for email and SMS, using Blob storage for persistence.
- **TestAzure.DurableFunctions**: Example of Durable Functions (orchestrations, activities) for demonstration.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools (v4)](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Azure Storage account (or [Azurite emulator](https://github.com/Azure/Azurite))
- Azure Service Bus namespace (or [Service Bus emulator](https://github.com/Azure/azure-service-bus-emulator))

## Configuration

Each function project has a `local.settings.json` under its folder:

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "<YourStorageConnectionString>",
    "ServiceBusConnection": "<YourServiceBusConnectionString>",
    "Functions__Worker__HostEndpoint": "http://localhost:7071"
  }
}
```

Ensure you update the connection strings before running locally.

## Running Locally

1. Open a terminal and navigate to each function project in turn:
   ```powershell
   cd "c:\Sources\My Projects\TestAzureFeatures\TestAzure.AcceptingOrders"
   func start
   ```
2. Repeat for **TestAzure.WebFunctions**, **TestAzure.ProcessOrders**, and **TestAzure.QueueFunctions**.
3. For Durable Functions, run the **TestAzure.DurableFunctions** project similarly.

Alternatively, you can build and run via `dotnet run` in each project folder once the Functions host endpoint is configured in `local.settings.json`.

## Deployment

Use your preferred CI/CD or Azure CLI:

```powershell
# Example: publish AcceptingOrders
cd "TestAzure.AcceptingOrders"
func azure functionapp publish <FunctionAppName>
```

Repeat for the other function apps.

## Storage Tables & Service Bus Entities

- **Table Storage**:
  - `items`: stores item definitions
  - `orders`: stores order entities
  - `ordererrors`: records failed orders
- **Service Bus**:
  - Queue: `orders-queue` (ingest new orders)
  - Topic: `order-created` (broadcast completed orders)
  - Subscriptions:
    - `email` (handled by EmailNotificationService)
    - `reports` (handled by reporting subscriber)

## Extensions & Packages

Key NuGet packages used:

- `Microsoft.Azure.Functions.Worker` / `Worker.Sdk`
- `Microsoft.Azure.Functions.Worker.Extensions.Http`
- `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`
- `Azure.Data.Tables`
- `Azure.Messaging.ServiceBus`
- `Azure.Storage.Blobs`

## Contributing

Feel free to submit issues or pull requests. Ensure coding standards and unit tests are in place.


