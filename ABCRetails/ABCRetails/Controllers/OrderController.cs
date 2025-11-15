using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABCRetails.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ISqlDataService _dataService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ISqlDataService dataService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _dataService = dataService;
            _logger = logger;
        }

        // GET: Order - Show different views based on role
        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.IsInRole("Admin"))
                {
                    var allOrders = await _orderService.GetOrderViewModelsAsync();

                    // Debug logging to check what's being returned
                    _logger.LogInformation("Admin Index: Retrieved {Count} orders", allOrders?.Count ?? 0);

                    if (allOrders == null || !allOrders.Any())
                    {
                        _logger.LogWarning("No orders found for admin view");
                        TempData["Info"] = "No orders found in the system.";
                    }

                    return View("AdminIndex", allOrders ?? new List<OrderViewModel>());
                }
                else
                {
                    var customerOrders = await GetCustomerOrdersAsync();

                    // Debug logging
                    _logger.LogInformation("Customer Index: Retrieved {Count} orders for customer", customerOrders?.Count ?? 0);

                    if (customerOrders == null || !customerOrders.Any())
                    {
                        _logger.LogInformation("No orders found for current customer");
                        TempData["Info"] = "You haven't placed any orders yet.";
                    }

                    return View("CustomerIndex", customerOrders ?? new List<OrderViewModel>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Error loading orders. Please try again.";
                return View(new List<OrderViewModel>());
            }
        }

        // GET: Order/Create - Only for customers (UPDATED FOR CART)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get cart data from query string
                var cartData = Request.Query["cart"];
                List<CartItemViewModel> cartItems = new List<CartItemViewModel>();
                decimal totalAmount = 0;

                if (!string.IsNullOrEmpty(cartData))
                {
                    try
                    {
                        var cartJson = System.Net.WebUtility.UrlDecode(cartData);
                        var cart = System.Text.Json.JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
                        cartItems = cart ?? new List<CartItemViewModel>();

                        // Calculate total amount
                        totalAmount = cartItems.Sum(item => item.Price * item.Quantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing cart data");
                        TempData["Error"] = "Error loading cart data. Please try again.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Your cart is empty. Please add products to your cart first.";
                    return RedirectToAction("Index", "Home");
                }

                // Get current customer
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _dataService.GetCustomerByEmailAsync(userEmail);

                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found. Please contact support.";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new OrderCreateViewModel
                {
                    OrderDate = DateTime.Today,
                    Status = "Pending",
                    ShippingAddress = customer.ShippingAddress,
                    CustomerId = customer.Id,
                    CartItems = cartItems,
                    TotalAmount = totalAmount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order page");
                TempData["Error"] = "Error loading order form. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Order/Create - Only for customers (UPDATED FOR CART)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            _logger.LogInformation("Creating order for customer: {CustomerId}", model.CustomerId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid for order creation");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Model error: {Error}", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                // Get current customer
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _dataService.GetCustomerByEmailAsync(userEmail);

                if (customer == null)
                {
                    ModelState.AddModelError("", "Customer profile not found. Please contact support.");
                    return View(model);
                }

                // Validate cart items and check stock
                foreach (var cartItem in model.CartItems)
                {
                    var product = await _dataService.GetProductAsync(cartItem.Id);
                    if (product == null)
                    {
                        ModelState.AddModelError("", $"Product '{cartItem.Name}' not found.");
                        return View(model);
                    }

                    if (product.StockAvailable < cartItem.Quantity)
                    {
                        ModelState.AddModelError("", $"Insufficient stock for '{cartItem.Name}'. Only {product.StockAvailable} available.");
                        return View(model);
                    }
                }

                // Create the order
                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = model.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = model.TotalAmount,
                    Status = "Pending",
                    ShippingAddress = model.ShippingAddress,
                    CustomerNotes = model.CustomerNotes,
                    OrderItems = new List<OrderItem>()
                };

                // Add order items
                foreach (var cartItem in model.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = cartItem.Id,
                        ProductName = cartItem.Name,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Price,
                        TotalPrice = cartItem.Price * cartItem.Quantity
                    };
                    order.OrderItems.Add(orderItem);

                    // Update product stock
                    var product = await _dataService.GetProductAsync(cartItem.Id);
                    product.StockAvailable -= cartItem.Quantity;
                    await _dataService.UpdateProductAsync(product.Id, product);
                }

                var createdOrder = await _dataService.CreateOrderAsync(order);

                // Clear the cart after successful order creation
                Response.Cookies.Delete("customerCart");

                TempData["Success"] = $"Order #{createdOrder.Id} submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError(string.Empty, $"Error creating order: {ex.Message}");
                return View(model);
            }
        }

        // GET: Order/Details/5 - Both can view, but customers only their own
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var orderDetail = await _orderService.GetOrderDetailViewModelAsync(id);
                if (orderDetail == null)
                {
                    return NotFound();
                }

                // If user is customer, check if they own this order
                if (User.IsInRole("Customer"))
                {
                    var customerOrders = await GetCustomerOrdersAsync();
                    if (!customerOrders.Any(o => o.Id == id))
                    {
                        TempData["Error"] = "You can only view your own orders.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                return View(orderDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for ID: {Id}", id);
                TempData["Error"] = "Error loading order details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Order/Edit/5 - Only for admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var order = await _dataService.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit view for order ID: {Id}", id);
                TempData["Error"] = $"Error loading order for edit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Edit/5 - Only for admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _dataService.UpdateOrderStatusAsync(id, order.Status, User.Identity?.Name);
                    _logger.LogInformation("Order {Id} status updated to {Status} via Edit POST", id, order.Status);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating order ID: {Id} via Edit POST", id);
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        // POST: Order/UpdateOrderStatus - Only for admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                await _dataService.UpdateOrderStatusAsync(id, newStatus, User.Identity?.Name);
                _logger.LogInformation("Order {Id} status updated to {NewStatus} via direct action", id, newStatus);
                TempData["Success"] = $"Order {id} status updated to {newStatus}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for ID: {Id} to {NewStatus}", id, newStatus);
                TempData["Error"] = $"Error updating order status: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Order/CancelOrder - Both can cancel, but customers only their own
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(string id)
        {
            try
            {
                if (User.IsInRole("Customer"))
                {
                    var customerOrders = await GetCustomerOrdersAsync();
                    if (!customerOrders.Any(o => o.Id == id))
                    {
                        TempData["Error"] = "You can only cancel your own orders.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                await _dataService.UpdateOrderStatusAsync(id, "Cancelled", User.Identity?.Name);
                _logger.LogInformation("Order {Id} successfully cancelled", id);
                TempData["Success"] = $"Order #{id} has been cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order ID: {Id}", id);
                TempData["Error"] = $"Error cancelling order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Order/Delete/5 - Only for admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _dataService.DeleteOrderAsync(id);
                _logger.LogInformation("Order {Id} successfully deleted", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order ID: {Id}", id);
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to get customer's orders - IMPROVED VERSION
        private async Task<List<OrderViewModel>> GetCustomerOrdersAsync()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _dataService.GetCustomerByEmailAsync(userEmail);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for user email: {Email}", userEmail);
                    return new List<OrderViewModel>();
                }

                var allOrders = await _orderService.GetOrderViewModelsAsync();

                if (allOrders == null)
                {
                    _logger.LogWarning("No orders returned from order service");
                    return new List<OrderViewModel>();
                }

                // ✅ FIX: Filter by CustomerId instead of Email
                var customerOrders = allOrders.Where(o => o.CustomerId == customer.Id).ToList();

                _logger.LogInformation("Found {Count} orders for customer {Email}",
                    customerOrders.Count, userEmail);

                return customerOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer orders");
                return new List<OrderViewModel>();
            }
        }

    }
}