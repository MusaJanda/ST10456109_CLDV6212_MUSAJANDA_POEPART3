using ABCRetails.Models;
using ABCRetails.Models.ViewModels;

namespace ABCRetails.Services
{
    public interface IOrderService
    {
        Task<List<OrderViewModel>> GetOrderViewModelsAsync();
        Task<OrderDetailViewModel?> GetOrderDetailViewModelAsync(string id);

        // Optional: Remove these if not used
        Task<Order> CreateOrderAsync(string customerId, string shippingAddress, string customerNotes = null);
        Task<bool> ProcessOrderAsync(string orderId, string processedBy);
        Task<List<Order>> GetCustomerOrdersAsync(string customerId);
    }
}