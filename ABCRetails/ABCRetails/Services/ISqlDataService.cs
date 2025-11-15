using ABCRetails.Models;

namespace ABCRetails.Services
{
    public interface ISqlDataService
    {
        // Customer methods
        Task<List<Customer>> GetAllCustomersAsync();
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<Customer?> GetCustomerByUsernameAsync(string username);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(string id, Customer customer);
        Task<bool> DeleteCustomerAsync(string id);
        Task<int> GetCustomerCountAsync();

        // Product methods
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(string id, Product product);
        Task<bool> DeleteProductAsync(string id);
        Task<int> GetProductCountAsync();

        // Order methods
        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<Order> CreateOrderAsync(Order order);
        Task<bool> UpdateOrderAsync(string id, Order order);
        Task<bool> DeleteOrderAsync(string id);
        Task<bool> UpdateOrderStatusAsync(string id, string status, string processedBy = null);
        Task<int> GetOrderCountAsync();

        // Order Item methods
        Task<List<OrderItem>> GetOrderItemsAsync(string orderId);
        Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem);
        Task<bool> DeleteOrderItemAsync(string id);

        // Shopping Cart methods
        Task<ShoppingCart?> GetShoppingCartAsync(string customerId);
        Task<ShoppingCart> CreateShoppingCartAsync(ShoppingCart cart);
        Task<bool> UpdateShoppingCartAsync(ShoppingCart cart);
        Task<bool> ClearShoppingCartAsync(string customerId);
        Task<bool> ClearCartAsync(string customerId);

        // Cart Item methods
        Task<List<CartItem>> GetCartItemsAsync(string cartId);
        Task<CartItem> CreateCartItemAsync(CartItem cartItem);
        Task<bool> UpdateCartItemAsync(CartItem cartItem);
        Task<bool> UpdateCartItemAsync(string cartItemId, int quantity);
        Task<bool> DeleteCartItemAsync(string id);
        Task<bool> ClearCartItemsAsync(string cartId);
        Task<bool> AddToCartAsync(string customerId, string productId, int quantity);
        Task<bool> RemoveFromCartAsync(string customerId, string productId);
    }
}