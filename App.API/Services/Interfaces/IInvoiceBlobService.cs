namespace App.API.Services.Interfaces
{
    /// <summary>
    /// Uploads invoice files for an order to Azure Blob Storage.
    /// </summary>
    public interface IInvoiceBlobService
    {
        /// <summary>
        /// Uploads an invoice file under the order identifier prefix.
        /// </summary>
        /// <param name="orderId">Order whose invoice is being stored.</param>
        /// <param name="file">Uploaded invoice file from the HTTP request.</param>
        /// <param name="ct">Token used to cancel the upload.</param>
        /// <returns>
        /// <see langword="true"/> when the blob write succeeds.
        /// </returns>
        /// <exception cref="Exception">
        /// Propagated when the storage client cannot complete the upload.
        /// </exception>
        Task<bool> UploadAsync(Guid orderId, IFormFile file, CancellationToken ct = default);
    }
}
