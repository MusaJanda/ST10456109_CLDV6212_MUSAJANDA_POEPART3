using Azure;
using Azure.Data.Tables;

namespace ABCRetailsFunctions.Entities
{
    public class ProductEntity : ITableEntity
    {
        public ProductEntity()
        {
            RowKey = string.Empty;
        }

        public ProductEntity(string rowKey)
        {
            RowKey = rowKey ?? string.Empty;
        }

        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // ADDED: Id property to match web app model
        public string Id { get; set; } = string.Empty;
    }
}