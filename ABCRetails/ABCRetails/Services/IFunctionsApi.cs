using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetails.Models;
using ABCRetailsFunctions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailsFunctions.Services
{
    // ##################################################################
    // ---------- INTERFACE DEFINITION (IFunctionsApi) ----------
    // ##################################################################
    public interface IFunctionsApi
    {
        // Customers (Model-based methods)
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer> CreateCustomerAsync(Customer c);
        Task<Customer> UpdateCustomerAsync(string id, Customer c);
        Task DeleteCustomerAsync(string id);

        // Customers (DTO-based methods - Note: Model-based retrieval kept to avoid signature conflict)
        Task CreateCustomerAsync(CustomerDto customerDto); // Matches client implementation (returns Task)
        Task UpdateCustomerAsync(string rowKey, CustomerDto customerDto); // Matches client implementation (returns Task)

        // Products (Model-based methods - PRIMARY)
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
        Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
        Task DeleteProductAsync(string id);

        // Products (DTO-based methods - ALTERNATIVE)
        Task<ProductDto> CreateProductAsync(ProductDto productDto, IFormFile? imageFile);
        Task<ProductDto> UpdateProductAsync(string rowKey, ProductDto productDto, IFormFile? imageFile);

        // Orders
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
        Task UpdateOrderStatusAsync(string id, string newStatus);
        Task DeleteOrderAsync(string id);

        // Uploads
        Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);
    }
}