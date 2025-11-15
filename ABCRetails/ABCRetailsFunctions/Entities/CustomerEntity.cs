using Azure;
using Azure.Data.Tables;

namespace ABCRetailsFunctions.Entities
{
    public class CustomerEntity : ITableEntity
    {
        public CustomerEntity()
        {
            RowKey = string.Empty;
        }

        public CustomerEntity(string rowKey)
        {
            RowKey = rowKey ?? string.Empty;
        }

        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        // ADDED: Id property to match web app model
        public string Id { get; set; } = string.Empty;
    }
}