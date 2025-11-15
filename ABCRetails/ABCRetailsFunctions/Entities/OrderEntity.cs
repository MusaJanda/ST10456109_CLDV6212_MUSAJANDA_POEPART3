using Azure;
using Azure.Data.Tables;

namespace ABCRetailsFunctions.Entities
{
    public class OrderEntity : ITableEntity
    {
        public OrderEntity()
        {
            RowKey = string.Empty;
        }

        public OrderEntity(string rowKey)
        {
            RowKey = rowKey ?? string.Empty;
        }

        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Submitted";

        // ADDED: Id property to match web app model
        public string Id { get; set; } = string.Empty;
    }
}