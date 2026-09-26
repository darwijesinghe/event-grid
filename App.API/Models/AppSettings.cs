namespace App.API.Models
{
    /// <summary>
    /// Root configuration object bound from application settings.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Custom Event Grid topic endpoint, key, and CloudEvents toggle.
        /// </summary>
        public EventGridOptions EventGridOptions       { get; set; }

        /// <summary>
        /// Azure SQL connection used by <c>AppDbContext</c>.
        /// </summary>
        public AppDbContextOptions AppDbContextOptions { get; set; }

        /// <summary>
        /// Blob storage connection and invoice container name.
        /// </summary>
        public AzureStorageOptions AzureStorageOptions { get; set; }
    }

    /// <summary>
    /// Connection settings for the orders database.
    /// </summary>
    public sealed class AppDbContextOptions
    {
        /// <summary>
        /// Azure SQL connection string for Entity Framework Core.
        /// </summary>
        public string ConnectionString { get; set; } = "";
    }

    /// <summary>
    /// Connection settings for Azure Blob Storage invoice uploads.
    /// </summary>
    public sealed class AzureStorageOptions 
    {
        /// <summary>
        /// Storage account connection string used to create a <c>BlobServiceClient</c>.
        /// </summary>
        public string ConnectionString { get; set; } = "";

        /// <summary>
        /// Container that stores invoice blobs under <c>{orderId}/{fileName}</c>.
        /// </summary>
        public string ContainerName    { get; set; } = "";
    }

    /// <summary>
    /// Publisher settings for the custom Event Grid topic.
    /// </summary>
    public sealed class EventGridOptions
    {
        /// <summary>
        /// HTTPS endpoint of the custom Event Grid topic.
        /// </summary>
        public string TopicEndpoint { get; set; } = "";

        /// <summary>
        /// Access key used to authenticate the Event Grid publisher client.
        /// </summary>
        public string TopicKey      { get; set; } = "";

        /// <summary>
        /// When true, events are sent as CloudEvents instead of Event Grid schema.
        /// </summary>
        public bool UseCloudEvents  { get; set; }
    }
}
