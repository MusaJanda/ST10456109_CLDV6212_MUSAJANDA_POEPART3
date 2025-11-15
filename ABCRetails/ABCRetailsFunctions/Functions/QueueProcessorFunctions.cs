using ABCRetailsFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace ABCRetailsFunctions.Functions;

public class QueueProcessorFunctions
{
    private readonly ILogger<QueueProcessorFunctions> _logger;

    public QueueProcessorFunctions(ILogger<QueueProcessorFunctions> logger)
    {
        _logger = logger;
        _logger.LogInformation("✅ QueueProcessorFunctions initialized - Ready to process queue messages");
    }

    [Function("OrderNotifications_Processor")]
    public async Task OrderNotificationsProcessor(
        [QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%", Connection = "AzureWebJobsStorage")] string message,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("OrderNotifications_Processor");

        try
        {
            _logger.LogInformation("📨 ORDER NOTIFICATION - Message received from queue");
            log.LogInformation($"📨 Context Logger - Order notification message received");

            // Parse the message to extract details
            try
            {
                var notification = JsonSerializer.Deserialize<OrderNotificationMessage>(message);
                if (notification != null)
                {
                    _logger.LogInformation("📋 ORDER NOTIFICATION - Processing: {MessageType}, Order: {OrderId}, Status: {NewStatus} → {PreviousStatus}",
                        notification.Type, notification.OrderId, notification.NewStatus, notification.PreviousStatus);

                    log.LogInformation($"📋 Context Logger - Order {notification.OrderId} status changed from {notification.PreviousStatus} to {notification.NewStatus}");
                }
                else
                {
                    _logger.LogWarning("⚠️ ORDER NOTIFICATION - Could not parse message as OrderNotification");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning("⚠️ ORDER NOTIFICATION - Message is not valid JSON: {RawMessage}. Error: {ErrorMessage}",
                    message, jsonEx.Message);
            }

            // Simulate processing logic
            _logger.LogInformation("🔄 ORDER NOTIFICATION - Processing order notification...");

            // Example: Send email notification, update audit log, etc.
            // await _emailService.SendOrderStatusUpdate(notification);

            // Simulate async work
            await Task.Delay(100); // Small delay to simulate processing

            _logger.LogInformation("✅ ORDER NOTIFICATION - Successfully processed order notification");
            log.LogInformation($"✅ Context Logger - Order notification processing completed");

            // Log completion with message details
            _logger.LogInformation("🏁 ORDER NOTIFICATION - Completed processing message: {MessageId}",
                ctx.InvocationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ORDER NOTIFICATION - Error processing message: {ErrorMessage}", ex.Message);
            log.LogError($"❌ Context Logger - Error processing order notification: {ex.Message}");

            // Re-throw to enable retry mechanism
            throw;
        }
        finally
        {
            _logger.LogInformation("📬 ORDER NOTIFICATION - Message processing finalized");
        }
    }

    [Function("StockUpdates_Processor")]
    public async Task StockUpdatesProcessor(
        [QueueTrigger("%QUEUE_STOCK_UPDATES%", Connection = "AzureWebJobsStorage")] string message,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("StockUpdates_Processor");

        try
        {
            _logger.LogInformation("📨 STOCK UPDATE - Message received from queue");
            log.LogInformation($"📨 Context Logger - Stock update message received");

            // Parse the message to extract details
            try
            {
                var stockUpdate = JsonSerializer.Deserialize<StockUpdateMessage>(message);
                if (stockUpdate != null)
                {
                    _logger.LogInformation("📦 STOCK UPDATE - Processing: Product: {ProductId}, Quantity Change: {QuantityChange}",
                        stockUpdate.ProductId, stockUpdate.QuantityChange);

                    log.LogInformation($"📦 Context Logger - Product {stockUpdate.ProductId} stock change: {stockUpdate.QuantityChange}");

                    // Determine if it's a restock or sale
                    var operationType = stockUpdate.QuantityChange > 0 ? "RESTOCK" : "SALE";
                    _logger.LogInformation("📊 STOCK UPDATE - Operation: {OperationType}, Absolute Change: {AbsoluteChange}",
                        operationType, Math.Abs(stockUpdate.QuantityChange));
                }
                else
                {
                    _logger.LogWarning("⚠️ STOCK UPDATE - Could not parse message as StockUpdate");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning("⚠️ STOCK UPDATE - Message is not valid JSON: {RawMessage}. Error: {ErrorMessage}",
                    message, jsonEx.Message);
            }

            // Simulate stock update processing
            _logger.LogInformation("🔄 STOCK UPDATE - Updating inventory levels...");

            // Example: Update database, trigger low stock alerts, etc.
            // await _inventoryService.UpdateStockLevels(stockUpdate);

            // Simulate async work
            await Task.Delay(100); // Small delay to simulate processing

            // Simulate business logic
            _logger.LogInformation("📈 STOCK UPDATE - Inventory levels updated successfully");

            // Check for low stock scenarios
            // if (newStockLevel < lowStockThreshold) 
            // {
            //     _logger.LogWarning("⚠️ STOCK UPDATE - Low stock alert for product: {ProductId}", stockUpdate.ProductId);
            // }

            _logger.LogInformation("✅ STOCK UPDATE - Successfully processed stock update");
            log.LogInformation($"✅ Context Logger - Stock update processing completed");

            // Log completion with message details
            _logger.LogInformation("🏁 STOCK UPDATE - Completed processing message: {MessageId}",
                ctx.InvocationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ STOCK UPDATE - Error processing message: {ErrorMessage}", ex.Message);
            log.LogError($"❌ Context Logger - Error processing stock update: {ex.Message}");

            // Re-throw to enable retry mechanism
            throw;
        }
        finally
        {
            _logger.LogInformation("📬 STOCK UPDATE - Message processing finalized");
        }
    }

    // Helper classes for message deserialization
    private class OrderNotificationMessage
    {
        public string Type { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTimeOffset UpdatedDateUtc { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    private class StockUpdateMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
    }

    // Optional: Add a method to process poison queue messages (dead letter queue)
    [Function("ProcessPoisonMessages")]
    public async Task ProcessPoisonMessages(
        [QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%-poison", Connection = "AzureWebJobsStorage")] string poisonMessage,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("ProcessPoisonMessages");

        _logger.LogError("☠️ POISON MESSAGE - Processing failed message from poison queue");
        log.LogError($"☠️ Context Logger - Poison message: {poisonMessage}");

        // Log the poison message for manual intervention
        _logger.LogWarning("⚠️ POISON MESSAGE - Manual intervention required for message: {PoisonMessage}",
            poisonMessage);

        // You could also send an alert email here
        // await _alertService.SendPoisonMessageAlert(poisonMessage);

        // Simulate async work
        await Task.Delay(100);
    }
}