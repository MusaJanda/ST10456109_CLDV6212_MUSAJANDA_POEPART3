using System.Security.Claims;
using ABCRetails.Services;
using ABCRetails.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCRetails.Data;

namespace ABCRetails.Controllers
{
    public class CartController : Controller
    {
        private readonly ISqlDataService _dataService;
        private readonly ILogger<CartController> _logger;
        private readonly AppDbContext _context; // FIXED: Changed from ApplicationDbContext to AppDbContext

        public CartController(ISqlDataService dataService, ILogger<CartController> logger, AppDbContext context)
        {
            _dataService = dataService;
            _logger = logger;
            _context = context;
        }

        // ... rest of your CartController code remains the same ...
    }
}