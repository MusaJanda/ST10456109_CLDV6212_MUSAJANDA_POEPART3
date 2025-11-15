using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ABCRetails.Models;
using ABCRetails.Services;
using ABCRetails.Models.ViewModels;

namespace ABCRetails.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Authenticate using Customer table
                var customer = await _authService.AuthenticateAsync(model.Email, model.Password);

                if (customer == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    _logger.LogWarning("Failed login attempt for {Email}", model.Email);
                    return View(model);
                }

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.Id),
                    new Claim(ClaimTypes.Name, customer.FullName),
                    new Claim(ClaimTypes.Email, customer.Email),
                    new Claim(ClaimTypes.Role, customer.Role),
                    new Claim("Username", customer.Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Email} logged in successfully as {Role}",
                    model.Email, customer.Role);

                // Redirect based on role
                if (customer.Role == "Admin")
                {
                    return RedirectToAction("AdminDashboard", "Home");
                }
                else
                {
                    return RedirectToAction("Dashboard", "Customer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        // GET: Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            Console.WriteLine("Register POST method called"); // Debug
            _logger.LogInformation("Register POST method called for {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid for registration");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                _logger.LogInformation("Attempting to register user: {Email}", model.Email);
                var success = await _authService.RegisterAsync(model);

                if (!success)
                {
                    ModelState.AddModelError("", "Email already registered or registration failed");
                    _logger.LogWarning("Registration failed for {Email}", model.Email);
                    return View(model);
                }

                TempData["Success"] = "Registration successful! Please login with your credentials.";
                _logger.LogInformation("New customer registered: {Email}", model.Email);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        // POST: Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {UserName} logged out", userName);

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}