using System.Diagnostics;
using System.Security.Claims;
using ABCRetails.Models;
using ABCRetails.Models.ViewModels;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly IAuthService _authService;
        private readonly IOrderService _orderService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ISqlDataService sqlDataService, IAuthService authService, IOrderService orderService, ILogger<HomeController> logger)
        {
            _sqlDataService = sqlDataService;
            _authService = authService;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _sqlDataService.GetProductsAsync();

                // Filter for featured products (active and in stock)
                var featuredProducts = products?
                    .Where(p => p.IsActive && p.StockAvailable > 0)
                    .Take(8)
                    .ToList() ?? new List<Product>();

                // Check if user is authenticated and get role
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                var isAdmin = User.IsInRole("Admin");

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = featuredProducts,
                    ProductCount = products?.Count ?? 0,
                    CustomerCount = isAdmin ? await _sqlDataService.GetCustomerCountAsync() : 0,
                    OrderCount = isAdmin ? await _sqlDataService.GetOrderCountAsync() : 0,
                    IsAdmin = isAdmin,
                    IsAuthenticated = isAuthenticated
                };

                _logger.LogInformation("Loaded home page - {ProductCount} products, {FeaturedCount} featured",
                    viewModel.ProductCount, viewModel.FeaturedProducts.Count);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");
                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = new List<Product>(),
                    ProductCount = 0,
                    CustomerCount = 0,
                    OrderCount = 0,
                    IsAdmin = false,
                    IsAuthenticated = false
                };
                return View(viewModel);
            }
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                return RedirectToAction("CustomerDashboard");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                ViewData["Title"] = "Admin Dashboard";

                // Get dashboard stats
                var productCount = await _sqlDataService.GetProductCountAsync();
                var customerCount = await _sqlDataService.GetCustomerCountAsync();
                var orderCount = await _sqlDataService.GetOrderCountAsync();

                var viewModel = new AdminDashboardViewModel
                {
                    TotalProducts = productCount,
                    TotalCustomers = customerCount,
                    TotalOrders = orderCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var customer = await _sqlDataService.GetCustomerByEmailAsync(userEmail);

                var products = await _sqlDataService.GetProductsAsync();
                var customerOrders = await _orderService.GetOrderViewModelsAsync();
                customerOrders = customerOrders.Where(o => o.Email == userEmail).ToList();

                var viewModel = new CustomerDashboardViewModel
                {
                    FeaturedProducts = products?.Where(p => p.IsActive && p.StockAvailable > 0).ToList() ?? new List<Product>(),
                    OrderCount = customerOrders.Count,
                    PendingOrders = customerOrders.Count(o => o.Status == "Pending"),
                    CartItemCount = 0 // This will be handled by JavaScript
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer dashboard");
                TempData["Error"] = "Error loading dashboard. Please try again.";
                return View(new CustomerDashboardViewModel());
            }
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View("ContactUs");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}