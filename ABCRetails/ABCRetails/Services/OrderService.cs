using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace ABCRetails.Services
{
    public class OrderService : IOrderService
    {
        private readonly ISqlDataService _dataService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ISqlDataService dataService, ILogger<OrderService> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        public async Task<List<OrderViewModel>> GetOrderViewModelsAsync()
        {
            try
            {
                _logger.LogInformation("Getting order view models");

                var orders = await _dataService.GetOrdersAsync();

                var orderViewModels = orders.Select(order => new OrderViewModel
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId, // ADD THIS LINE
                    OrderDate = order.OrderDate,
                    CustomerName = $"{order.Customer?.Name} {order.Customer?.Surname}",
                    Username = order.Customer?.Username ?? "Unknown",
                    Email = order.Customer?.Email ?? "Unknown",
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ShippingAddress = order.ShippingAddress,
                    ItemsCount = order.OrderItems?.Count ?? 0,
                    TotalQuantity = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
                    ProductNames = string.Join(", ", order.OrderItems?.Select(oi => oi.ProductName).Take(3) ?? new List<string>())
                }).ToList();

                _logger.LogInformation("Retrieved {Count} order view models", orderViewModels.Count);
                return orderViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order view models");
                return new List<OrderViewModel>();
            }
        }

        public async Task<OrderDetailViewModel?> GetOrderDetailViewModelAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting order detail view model for order: {OrderId}", id);

                var order = await _dataService.GetOrderAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", id);
                    return null;
                }

                var orderDetailViewModel = new OrderDetailViewModel
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    CustomerId = order.CustomerId,
                    CustomerName = $"{order.Customer?.Name} {order.Customer?.Surname}",
                    Username = order.Customer?.Username ?? "Unknown",
                    Email = order.Customer?.Email ?? "Unknown",
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ShippingAddress = order.ShippingAddress,
                    CustomerNotes = order.CustomerNotes,
                    ProcessedDate = order.ProcessedDate,
                    ProcessedBy = order.ProcessedBy,
                    OrderItems = order.OrderItems?.Select(oi => new OrderItemViewModel
                    {
                        ProductName = oi.ProductName,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList() ?? new List<OrderItemViewModel>()
                };

                _logger.LogInformation("Retrieved order detail view model for order: {OrderId}", id);
                return orderDetailViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order detail view model for order: {OrderId}", id);
                return null;
            }
        }

        // Remove these unused methods or implement them properly
        public Task<Order> CreateOrderAsync(string customerId, string shippingAddress, string customerNotes = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ProcessOrderAsync(string orderId, string processedBy)
        {
            throw new NotImplementedException();
        }

        public Task<List<Order>> GetCustomerOrdersAsync(string customerId)
        {
            throw new NotImplementedException();
        }
    }
}