using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using ABCRetails.Services;

namespace ABCRetails.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ISqlDataService _dataService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ISqlDataService dataService, ILogger<CustomerController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        // GET: Customer/Dashboard
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _dataService.GetCustomerByEmailAsync(userEmail);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for email: {Email}", userEmail);
                    TempData["Error"] = "Customer profile not found. Please contact support.";
                    return RedirectToAction("Index", "Home");
                }

                // Get customer's orders
                var allOrders = await _dataService.GetOrdersAsync();
                var customerOrders = allOrders?.Where(o => o.CustomerId == customer.Id).ToList()
                    ?? new List<Order>();

                // Get shopping cart
                var cart = await _dataService.GetShoppingCartAsync(customer.Id);

                var viewModel = new CustomerDashboardViewModel
                {
                    CustomerName = customer.FullName,
                    CustomerEmail = customer.Email,
                    CartItemCount = cart?.TotalItems ?? 0,
                    OrderCount = customerOrders.Count,
                    PendingOrders = customerOrders.Count(o => o.Status == "Pending" || o.Status == "Processing"),
                    TotalSpent = customerOrders
                        .Where(o => o.Status == "Delivered" || o.Status == "Shipped")
                        .Sum(o => o.TotalAmount)
                };

                _logger.LogInformation("Dashboard loaded for {CustomerName}: {OrderCount} orders, {PendingOrders} pending",
                    customer.FullName, viewModel.OrderCount, viewModel.PendingOrders);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer dashboard");
                TempData["Error"] = "Error loading dashboard. Please try again.";
                return View(new CustomerDashboardViewModel());
            }
        }

        // GET: Customer (Admin only - view all customers)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = await _dataService.GetCustomersAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                TempData["Error"] = $"Error loading customers: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        // GET: Customer/Details/5 (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var customer = await _dataService.GetCustomerAsync(id);
                if (customer == null)
                {
                    return NotFound();
                }

                // Get customer's orders
                var orders = await _dataService.GetOrdersAsync();
                var customerOrders = orders?.Where(o => o.CustomerId == id).ToList()
                    ?? new List<Order>();

                ViewBag.Orders = customerOrders;
                ViewBag.TotalSpent = customerOrders.Sum(o => o.TotalAmount);

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details for {CustomerId}", id);
                TempData["Error"] = $"Error loading customer: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customer/Edit/5 (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            try
            {
                var customer = await _dataService.GetCustomerAsync(id);
                if (customer == null)
                {
                    return NotFound();
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for edit: {CustomerId}", id);
                TempData["Error"] = $"Error loading customer for edit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Edit/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Customer customer)
        {
            if (string.IsNullOrEmpty(id) || id != customer.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                await _dataService.UpdateCustomerAsync(id, customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerId}", id);
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        // POST: Customer/Delete/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _dataService.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Profile (Customer can view/edit their own profile)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _dataService.GetCustomerByEmailAsync(userEmail);

                if (customer == null)
                {
                    return NotFound();
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                TempData["Error"] = "Error loading profile. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: Customer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile(Customer model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _dataService.GetCustomerAsync(userId);

                if (customer == null)
                {
                    return NotFound();
                }

                // Update only allowed fields
                customer.Name = model.Name;
                customer.Surname = model.Surname;
                customer.Phone = model.Phone;
                customer.ShippingAddress = model.ShippingAddress;

                await _dataService.UpdateCustomerAsync(userId, customer);

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["Error"] = "Error updating profile. Please try again.";
                return View(model);
            }
        }
    }
}