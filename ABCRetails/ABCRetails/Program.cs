using ABCRetails.Data;
using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ABCRetails.Session";
});

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Add database context
var connectionString = builder.Configuration.GetConnectionString("AzureSqlConnection")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Register services
    builder.Services.AddScoped<ISqlDataService, SqlDataService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<IOrderService, OrderService>();
}
else
{
    Console.WriteLine("Warning: No database connection string configured.");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add authentication middleware (must come before authorization)
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database - UPDATED: Create default admin CUSTOMER
// Initialize database - UPDATED: Create default admin CUSTOMER
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    try
    {
        // Apply pending migrations
        await dbContext.Database.MigrateAsync();

        // Create default admin CUSTOMER if none exists
        if (!dbContext.Customers.Any(u => u.Role == "Admin"))
        {
            var adminCustomer = new Customer
            {
                Name = "Admin",
                Surname = "User",
                Username = "admin",
                Email = "admin@abcretails.com",
                Phone = "000-000-0000",
                ShippingAddress = "Admin Address",
                PasswordHash = passwordHasher.HashPassword("Admin123!"),
                Role = "Admin",
                Status = "Active"
            };
            dbContext.Customers.Add(adminCustomer);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Default admin customer created: admin@abcretails.com / Admin123!");
        }

        Console.WriteLine("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not initialize database: {ex.Message}");
    }
}
await app.RunAsync();