using App.API.Controllers;
using App.API.Models;
using App.API.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace App.API.Services
{
    /// <summary>
    /// Uploads invoice files to Azure Blob Storage so Storage system events can notify subscribers.
    /// </summary>
    public class InvoiceBlobService : IInvoiceBlobService
    {
        private readonly ILogger<InvoiceBlobService> _logger;
        private readonly BlobContainerClient _container;
        private readonly AzureStorageOptions        _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceBlobService"/> class.
        /// </summary>
        /// <param name="logger">Logger used when an upload fails.</param>
        /// <param name="options">Application settings that include Azure Storage options.</param>
        public InvoiceBlobService(ILogger<InvoiceBlobService> logger, IOptions<AppSettings> options)
        {
            _logger     = logger;
            _options    = options.Value.AzureStorageOptions;
            var service = new BlobServiceClient(_options.ConnectionString);
            _container  = service.GetBlobContainerClient(_options.ContainerName);
        }

        /// <summary>
        /// Creates the invoice container if needed and overwrites the blob at <c>{orderId}/{fileName}</c>.
        /// </summary>
        /// <param name="orderId">Order whose invoice is being stored.</param>
        /// <param name="file">Uploaded invoice file from the HTTP request.</param>
        /// <param name="ct">Token used to cancel the upload.</param>
        /// <returns>
        /// <see langword="true"/> when the blob write succeeds.
        /// </returns>
        /// <exception cref="Exception">
        /// Rethrown after logging when the storage client fails.
        /// </exception>
        public async Task<bool> UploadAsync(Guid orderId, IFormFile file, CancellationToken ct = default)
        {
            try
            {
                // Private container: Event Grid still receives BlobCreated from the storage system topic.
                await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
                await using var stream = file.OpenReadStream();
                // Prefix by order id so invoice handlers can correlate the blob to an order.
                var blobClient         = _container.GetBlobClient($"{orderId}/{file.FileName}");
                await blobClient.UploadAsync(stream, overwrite: true, ct);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(UploadAsync));
                throw;
            }
        }
    }
}
