using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System.Text;
using TestAzure.Shared.Services;

namespace TestAzure.QueueFunctions.Services;

public abstract class BaseNotificationService(ILogger logger):BaseService(logger)
{
    public async Task StoreContentToBlob(string content, string container, CancellationToken cancellationToken = default)
    {
        var filename = $"{container}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        Logger.LogInformation("Storing content to blob. Container: {Container}, Filename: {Filename}", container, filename);

        var blobServiceClient = new BlobServiceClient(StorageConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(filename);
        Logger.LogDebug("Uploading content to blob '{BlobName}' in container '{Container}'.", filename, container);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, cancellationToken);

        Logger.LogInformation("Successfully uploaded blob '{BlobName}' to container '{Container}'.", filename, container);
    }
}
