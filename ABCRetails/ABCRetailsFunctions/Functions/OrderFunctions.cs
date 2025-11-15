using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetailsFunctions.Entities;
using ABCRetailsFunctions.Helpers;
using ABCRetailsFunctions.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailsFunctions.Functions;

public class OrderFunctions
{
    private readonly string _conn;
    private readonly string _ordersTable;
    private readonly string _productsTable;
    private readonly string _customersTable;
    private readonly string _queueOrder;
    private readonly string _queueStock;
    private readonly ILogger<OrderFunctions> _logger;

    public OrderFunctions(IConfiguration cfg, ILogger<OrderFunctions> logger)
    {
        _conn = cfg["AzureWebJobsStorage"]
                ?? cfg["ConnectionStrings:AzureStorage"]
                ?? throw new InvalidOperationException("AzureStorage connection string missing");

        _ordersTable = cfg["TABLE_ORDER"] ?? "Order";
        _productsTable = cfg["TABLE_PRODUCT"] ?? "Product";
        _customersTable = cfg["TABLE_CUSTOMER"] ?? "Customer";
        _queueOrder = cfg["QUEUE_ORDER_NOTIFICATIONS"] ?? "order-notifications";
        _queueStock = cfg["QUEUE_STOCK_UPDATES"] ?? "stock-updates";
        _logger = logger;

        _logger.LogInformation("✅ OrderFunctions initialized - Tables: {OrdersTable}, {ProductsTable}, {CustomersTable}",
            _ordersTable, _productsTable, _customersTable);
    }

    [Function("Orders_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("📥 LIST ORDERS - Request received");

        try
        {
            var table = new TableClient(_conn, _ordersTable);
            await table.CreateIfNotExistsAsync();
            _logger.LogInformation("🔗 Connected to orders table: {TableName}", _ordersTable);

            var items = new List<OrderDto>();
            var count = 0;

            await foreach (var e in table.QueryAsync<OrderEntity>(x => x.PartitionKey == "Order"))
            {
                items.Add(Map.ToDto(e));
                count++;
            }

            _logger.LogInformation("📤 LIST ORDERS - Returning {Count} orders", count);
            return await HttpJson.Ok(req, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LIST ORDERS - Error retrieving orders: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Internal server error");
        }
    }

    [Function("Orders_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 GET ORDER - Request for ID: {OrderId}", id);

        try
        {
            var table = new TableClient(_conn, _ordersTable);
            var resp = await table.GetEntityAsync<OrderEntity>("Order", id);

            _logger.LogInformation("✅ GET ORDER - Found order: {OrderId} with status: {Status}",
                id, resp.Value.Status);
            return await HttpJson.Ok(req, Map.ToDto(resp.Value));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ GET ORDER - Order not found: {OrderId}. Error: {ErrorMessage}", id, ex.Message);
            return await HttpJson.NotFound(req, "Order not found");
        }
    }

    private sealed record OrderCreate(string CustomerId, string ProductId, int Quantity);

    [Function("Orders_Create")]
    public async Task<HttpResponseData> Create(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("📥 CREATE ORDER - Request received");

        try
        {
            var input = await HttpJson.ReadAsync<OrderCreate>(req);
            if (input is null)
            {
                _logger.LogWarning("⚠️ CREATE ORDER - Invalid request body");
                return await HttpJson.BadRequest(req, "Invalid body");
            }

            // ✅ Ensure tables exist
            var products = new TableClient(_conn, _productsTable);
            await products.CreateIfNotExistsAsync();

            var customers = new TableClient(_conn, _customersTable);
            await customers.CreateIfNotExistsAsync();

            ProductEntity? product = null;

            try
            {
                // ✅ Try to get product by PartitionKey first
                var resp = await products.GetEntityAsync<ProductEntity>("Product", input.ProductId);
                product = resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // ✅ Fallback: query by RowKey only (in case PartitionKey differs)
                _logger.LogWarning("⚠️ Product not found by PartitionKey. Trying RowKey lookup for ID: {ProductId}", input.ProductId);
                await foreach (var p in products.QueryAsync<ProductEntity>(p => p.RowKey == input.ProductId))
                {
                    product = p;
                    break;
                }
            }

            if (product == null)
            {
                _logger.LogWarning("❌ CREATE ORDER - Product not found: {ProductId}", input.ProductId);
                return await HttpJson.BadRequest(req, $"Product with ID {input.ProductId} not found.");
            }

            if (product.StockAvailable < input.Quantity)
            {
                _logger.LogWarning("⚠️ CREATE ORDER - Insufficient stock for product: {ProductId}", input.ProductId);
                return await HttpJson.BadRequest(req, $"Insufficient stock. Available: {product.StockAvailable}, Requested: {input.Quantity}");
            }

            // ✅ Validate customer exists
            CustomerEntity? customer = null;
            try
            {
                var customerResp = await customers.GetEntityAsync<CustomerEntity>("Customer", input.CustomerId);
                customer = customerResp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                await foreach (var c in customers.QueryAsync<CustomerEntity>(c => c.RowKey == input.CustomerId))
                {
                    customer = c;
                    break;
                }
            }

            if (customer == null)
            {
                _logger.LogWarning("❌ CREATE ORDER - Customer not found: {CustomerId}", input.CustomerId);
                return await HttpJson.BadRequest(req, $"Customer with ID {input.CustomerId} not found.");
            }

            // ✅ Create order
            var orders = new TableClient(_conn, _ordersTable);
            await orders.CreateIfNotExistsAsync();

            var orderId = Guid.NewGuid().ToString("N");
            var orderEntity = Map.ToOrderEntity(
                customerId: input.CustomerId,
                productId: input.ProductId,
                productName: product.ProductName,
                quantity: input.Quantity,
                unitPrice: product.Price,
                rowKey: orderId
            );

            orderEntity.Username = customer.Username;
            await orders.AddEntityAsync(orderEntity);

            // ✅ Update product stock
            product.StockAvailable -= input.Quantity;
            await products.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

            // ✅ Return success
            _logger.LogInformation("✅ CREATE ORDER - Order {OrderId} created successfully for product {ProductId}", orderId, product.RowKey);
            return await HttpJson.Created(req, Map.ToDto(orderEntity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CREATE ORDER - Unexpected error: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Failed to create order");
        }
    }


    private sealed record OrderStatusUpdate(string Status);

    [Function("Orders_UpdateStatus")]
    public async Task<HttpResponseData> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "orders/{id}/status")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 UPDATE ORDER STATUS - Request for order: {OrderId}", id);

        try
        {
            var input = await HttpJson.ReadAsync<OrderStatusUpdate>(req);
            if (input is null || string.IsNullOrWhiteSpace(input.Status))
            {
                _logger.LogWarning("⚠️ UPDATE ORDER STATUS - Invalid status in body for order: {OrderId}", id);
                return await HttpJson.BadRequest(req, "Invalid status in body");
            }

            var orders = new TableClient(_conn, _ordersTable);
            var resp = await orders.GetEntityAsync<OrderEntity>("Order", id);
            var orderEntity = resp.Value;
            var previousStatus = orderEntity.Status;

            orderEntity.Status = input.Status;
            await orders.UpdateEntityAsync(orderEntity, orderEntity.ETag, TableUpdateMode.Replace);

            // Notify via queue
            var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await queueOrder.CreateIfNotExistsAsync();

            var statusMsg = new
            {
                Type = "OrderStatusUpdated",
                OrderId = orderEntity.RowKey,
                PreviousStatus = previousStatus,
                NewStatus = orderEntity.Status,
                UpdatedDateUtc = DateTimeOffset.UtcNow,
                UpdatedBy = "System",
                CustomerId = orderEntity.CustomerId,
                ProductId = orderEntity.ProductId
            };

            await queueOrder.SendMessageAsync(JsonSerializer.Serialize(statusMsg));

            _logger.LogInformation("✅ UPDATE ORDER STATUS - Successfully updated order {OrderId} from {PreviousStatus} to {NewStatus}",
                id, previousStatus, orderEntity.Status);

            return await HttpJson.Ok(req, Map.ToDto(orderEntity));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ UPDATE ORDER STATUS - Order not found or update failed: {OrderId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Order not found");
        }
    }

    [Function("Orders_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 DELETE ORDER - Request to delete order: {OrderId}", id);

        try
        {
            var table = new TableClient(_conn, _ordersTable);

            // First check if order exists and get its details for logging
            var existing = await table.GetEntityAsync<OrderEntity>("Order", id);
            _logger.LogInformation("🗑️ DELETE ORDER - Deleting order: {OrderId} with status: {Status}",
                id, existing.Value.Status);

            await table.DeleteEntityAsync("Order", id);

            // Notify about order deletion
            var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await queueOrder.CreateIfNotExistsAsync();

            var deleteMsg = new
            {
                Type = "OrderDeleted",
                OrderId = id,
                CustomerId = existing.Value.CustomerId,
                DeletedDateUtc = DateTimeOffset.UtcNow
            };

            await queueOrder.SendMessageAsync(JsonSerializer.Serialize(deleteMsg));

            _logger.LogInformation("✅ DELETE ORDER - Successfully deleted order: {OrderId}", id);
            return HttpJson.NoContent(req);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ DELETE ORDER - Order not found or delete failed: {OrderId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Order not found");
        }
    }
}