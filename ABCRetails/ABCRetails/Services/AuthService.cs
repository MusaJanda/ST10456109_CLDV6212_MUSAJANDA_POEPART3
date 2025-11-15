using ABCRetails.Data;
using ABCRetails.Models;
using ABCRetails.Models.ViewModels; // ADD THIS USING DIRECTIVE
using ABCRetails.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABCRetails.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<Customer?> AuthenticateAsync(string email, string password)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == email && c.Status == "Active");

                if (customer != null && _passwordHasher.VerifyPassword(password, customer.PasswordHash))
                {
                    customer.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Customer {Email} authenticated successfully", email);
                    return customer;
                }

                _logger.LogWarning("Authentication failed for {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating customer {Email}", email);
                return null;
            }
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Check if email exists
                if (await CustomerExistsAsync(model.Email))
                {
                    _logger.LogWarning("Registration failed: Email already exists {Email}", model.Email);
                    return false;
                }

                // Create Customer
                var customer = new Customer
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    Username = model.Username,
                    Email = model.Email,
                    Phone = model.Phone ?? "Not provided",
                    ShippingAddress = model.ShippingAddress,
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    Role = "Customer",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Create shopping cart for the new customer
                var shoppingCart = new ShoppingCart
                {
                    CustomerId = customer.Id,
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _context.ShoppingCarts.Add(shoppingCart);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {Email} registered successfully", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer {Email}", model.Email);
                return false;
            }
        }

        public async Task<bool> CustomerExistsAsync(string email)
        {
            return await _context.Customers
                .AnyAsync(c => c.Email == email && c.Status == "Active");
        }

        public async Task<Customer?> GetCustomerByIdAsync(string id)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.Status == "Active");
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email && c.Status == "Active");
        }
    }
}