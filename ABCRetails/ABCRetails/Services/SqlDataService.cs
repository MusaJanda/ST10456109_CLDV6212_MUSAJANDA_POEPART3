using ABCRetails.Data;
using ABCRetails.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABCRetails.Services
{
    public class SqlDataService : ISqlDataService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqlDataService> _logger;

        public SqlDataService(AppDbContext context, ILogger<SqlDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Customer Methods

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            try
            {
                return await _context.Customers
                    .Where(c => c.Status == "Active")
                    .OrderBy(c => c.Name)
                    .ThenBy(c => c.Surname)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return new List<Customer>();
            }
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await GetAllCustomersAsync();
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                return await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == "Active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {Id}", id);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            try
            {
                return await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Status == "Active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by email: {Email}", email);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            try
            {
                return await _context.Customers
                    .FirstOrDefaultAsync(c => c.Username.ToLower() == username.ToLower() && c.Status == "Active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by username: {Username}", username);
                return null;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            try
            {
                customer.Id ??= "CUST" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                customer.CreatedDate = DateTime.UtcNow;
                customer.Timestamp = DateTime.UtcNow;

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created customer: {CustomerId}", customer.Id);
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(string id, Customer customer)
        {
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                    return false;

                // Update properties
                existingCustomer.Name = customer.Name;
                existingCustomer.Surname = customer.Surname;
                existingCustomer.Username = customer.Username;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.ShippingAddress = customer.ShippingAddress;
                existingCustomer.Status = customer.Status;
                existingCustomer.LastLogin = customer.LastLogin;
                existingCustomer.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated customer: {CustomerId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                    return false;

                // Soft delete by changing status
                customer.Status = "Inactive";
                customer.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted customer: {CustomerId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {Id}", id);
                return false;
            }
        }

        public async Task<int> GetCustomerCountAsync()
        {
            try
            {
                return await _context.Customers
                    .Where(c => c.Status == "Active")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer count");
                return 0;
            }
        }

        #endregion

        #region Product Methods

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await GetAllProductsAsync();
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by ID: {Id}", id);
                return null;
            }
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            try
            {
                product.Id ??= "PROD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                product.Timestamp = DateTime.UtcNow;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created product: {ProductId}", product.Id);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<bool> UpdateProductAsync(string id, Product product)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                    return false;

                // Update properties
                existingProduct.ProductName = product.ProductName;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockAvailable = product.StockAvailable;
                existingProduct.Category = product.Category;
                existingProduct.ImageUrl = product.ImageUrl;
                existingProduct.IsActive = product.IsActive;
                existingProduct.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated product: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                // Soft delete
                product.IsActive = false;
                product.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted product: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {Id}", id);
                return false;
            }
        }

        public async Task<int> GetProductCountAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.IsActive)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product count");
                return 0;
            }
        }

        #endregion

        #region Order Methods

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                return new List<Order>();
            }
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            return await GetAllOrdersAsync();
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {Id}", id);
                return null;
            }
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                order.Id ??= "ORD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                order.OrderDate = DateTime.UtcNow;
                order.Timestamp = DateTime.UtcNow;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order: {OrderId}", order.Id);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<bool> UpdateOrderAsync(string id, Order order)
        {
            try
            {
                var existingOrder = await _context.Orders.FindAsync(id);
                if (existingOrder == null)
                    return false;

                // Update properties
                existingOrder.Status = order.Status;
                existingOrder.ShippingAddress = order.ShippingAddress;
                existingOrder.CustomerNotes = order.CustomerNotes;
                existingOrder.ProcessedBy = order.ProcessedBy;
                existingOrder.ProcessedDate = order.ProcessedDate;
                existingOrder.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated order: {OrderId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(string id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return false;

                // Remove order items first
                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted order: {OrderId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {Id}", id);
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, string status, string processedBy = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return false;

                order.Status = status;
                order.ProcessedBy = processedBy;
                if (status == "Processed" || status == "Shipped")
                {
                    order.ProcessedDate = DateTime.UtcNow;
                }
                order.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated order {OrderId} status to {Status}", id, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {Id} to {Status}", id, status);
                return false;
            }
        }

        public async Task<int> GetOrderCountAsync()
        {
            try
            {
                return await _context.Orders.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order count");
                return 0;
            }
        }

        #endregion

        #region Order Item Methods

        public async Task<List<OrderItem>> GetOrderItemsAsync(string orderId)
        {
            try
            {
                return await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order items for order: {OrderId}", orderId);
                return new List<OrderItem>();
            }
        }

        public async Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem)
        {
            try
            {
                orderItem.Id ??= "ORDITEM" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                return orderItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order item");
                throw;
            }
        }

        public async Task<bool> DeleteOrderItemAsync(string id)
        {
            try
            {
                var orderItem = await _context.OrderItems.FindAsync(id);
                if (orderItem == null)
                    return false;

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item: {Id}", id);
                return false;
            }
        }

        #endregion

        #region Shopping Cart Methods

        public async Task<ShoppingCart?> GetShoppingCartAsync(string customerId)
        {
            try
            {
                return await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(sc => sc.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shopping cart for customer: {CustomerId}", customerId);
                return null;
            }
        }

        public async Task<ShoppingCart> CreateShoppingCartAsync(ShoppingCart cart)
        {
            try
            {
                cart.Id ??= "CART" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                cart.CreatedDate = DateTime.UtcNow;
                cart.LastModified = DateTime.UtcNow;

                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shopping cart");
                throw;
            }
        }

        public async Task<bool> UpdateShoppingCartAsync(ShoppingCart cart)
        {
            try
            {
                cart.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shopping cart: {CartId}", cart.Id);
                return false;
            }
        }

        public async Task<bool> ClearShoppingCartAsync(string customerId)
        {
            try
            {
                var cart = await GetShoppingCartAsync(customerId);
                if (cart == null)
                    return false;

                _context.CartItems.RemoveRange(cart.CartItems);
                cart.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing shopping cart for customer: {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(string customerId)
        {
            return await ClearShoppingCartAsync(customerId);
        }

        #endregion

        #region Cart Item Methods

        public async Task<List<CartItem>> GetCartItemsAsync(string cartId)
        {
            try
            {
                return await _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Include(ci => ci.Product)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items for cart: {CartId}", cartId);
                return new List<CartItem>();
            }
        }

        public async Task<CartItem> CreateCartItemAsync(CartItem cartItem)
        {
            try
            {
                cartItem.Id ??= "CARTITEM" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                cartItem.AddedDate = DateTime.UtcNow;

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                return cartItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart item");
                throw;
            }
        }

        public async Task<bool> UpdateCartItemAsync(CartItem cartItem)
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item: {CartItemId}", cartItem.Id);
                return false;
            }
        }

        public async Task<bool> UpdateCartItemAsync(string cartItemId, int quantity)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(cartItemId);
                if (cartItem == null)
                    return false;

                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item quantity: {CartItemId}", cartItemId);
                return false;
            }
        }

        public async Task<bool> DeleteCartItemAsync(string id)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(id);
                if (cartItem == null)
                    return false;

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cart item: {Id}", id);
                return false;
            }
        }

        public async Task<bool> ClearCartItemsAsync(string cartId)
        {
            try
            {
                var cartItems = await GetCartItemsAsync(cartId);
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart items for cart: {CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> AddToCartAsync(string customerId, string productId, int quantity)
        {
            try
            {
                // Get or create shopping cart
                var cart = await GetShoppingCartAsync(customerId);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        CustomerId = customerId
                    };
                    await CreateShoppingCartAsync(cart);
                }

                // Check if product exists
                var product = await GetProductAsync(productId);
                if (product == null)
                    return false;

                // Check if item already exists in cart
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += quantity;
                    await UpdateCartItemAsync(existingItem);
                }
                else
                {
                    // Create new cart item
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    };
                    await CreateCartItemAsync(cartItem);
                }

                // Update cart last modified
                cart.LastModified = DateTime.UtcNow;
                await UpdateShoppingCartAsync(cart);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart for customer: {CustomerId}, product: {ProductId}", customerId, productId);
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(string customerId, string productId)
        {
            try
            {
                var cart = await GetShoppingCartAsync(customerId);
                if (cart == null)
                    return false;

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem == null)
                    return false;

                await DeleteCartItemAsync(cartItem.Id);

                // Update cart last modified
                cart.LastModified = DateTime.UtcNow;
                await UpdateShoppingCartAsync(cart);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart for customer: {CustomerId}, product: {ProductId}", customerId, productId);
                return false;
            }
        }

        #endregion
    }
}