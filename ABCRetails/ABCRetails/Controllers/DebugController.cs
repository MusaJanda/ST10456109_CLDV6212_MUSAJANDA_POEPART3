using ABCRetailsFunctions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ABCRetails.Controllers
{
    public class DebugController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<DebugController> _logger;

        public DebugController(IFunctionsApi functionsApi, ILogger<DebugController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        public async Task<IActionResult> CheckData()
        {
            try
            {
                var customers = await _functionsApi.GetCustomersAsync();
                var products = await _functionsApi.GetProductsAsync();

                ViewBag.Customers = customers;
                ViewBag.Products = products;

                _logger.LogInformation("Found {CustomerCount} customers and {ProductCount} products",
                    customers?.Count ?? 0, products?.Count ?? 0);

                return View();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error checking data");
                TempData["Error"] = $"Error: {ex.Message}";
                return View();
            }
        }
    }
}