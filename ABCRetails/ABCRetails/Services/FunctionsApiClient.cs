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

namespace ABCRetailsFunctions.Services;

public class FunctionsApiClient : IFunctionsApi
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly ILogger<FunctionsApiClient> _logger;

    private const string CustomersRoute = "customers";
    private const string ProductsRoute = "products";
    private const string OrdersRoute = "orders";
    private const string UploadsRoute = "uploads/proof-of-payment";

    public FunctionsApiClient(IHttpClientFactory factory, ILogger<FunctionsApiClient> logger)
    {
        _http = factory.CreateClient("Functions");
        _logger = logger;
    }

    // ---------- Helpers ----------
    private static HttpContent JsonBody(object obj)
        => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

    private async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
    {
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        _logger.LogInformation("API Response: {Content}", content);

        try
        {
            var data = JsonSerializer.Deserialize<T>(content, _json);
            if (data == null)
            {
                throw new JsonException("Deserialized data is null");
            }
            return data;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON: {Content}", content);
            throw new JsonException($"Failed to deserialize response: {ex.Message}. Response: {content}", ex);
        }
    }

    // ---------- Private Helper for Product Create/Update (Model & DTO) ----------
    private async Task<T> SendProductFormAsync<T>(string productName, string? description, decimal price, int stockAvailable, string? imageUrl, IFormFile? imageFile, string uri, HttpMethod method)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(productName), "ProductName");
        form.Add(new StringContent(description ?? string.Empty), "Description");
        form.Add(new StringContent(price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)), "Price");
        form.Add(new StringContent(stockAvailable.ToString()), "StockAvailable");

        if (!string.IsNullOrWhiteSpace(imageUrl))
            form.Add(new StringContent(imageUrl), "ImageUrl");

        if (imageFile is not null && imageFile.Length > 0)
        {
            var file = new StreamContent(imageFile.OpenReadStream());
            file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            form.Add(file, "ImageFile", imageFile.FileName);
        }

        HttpResponseMessage response;
        if (method == HttpMethod.Post)
        {
            response = await _http.PostAsync(uri, form);
        }
        else if (method == HttpMethod.Put)
        {
            response = await _http.PutAsync(uri, form);
        }
        else
        {
            throw new NotSupportedException($"HTTP method {method.Method} is not supported by this helper.");
        }

        return await ReadJsonAsync<T>(response);
    }

    // ##################################################################
    // ---------- Customers (Model-based methods) ----------
    // ##################################################################

    public async Task<List<Customer>> GetCustomersAsync()
    {
        try
        {
            _logger.LogInformation("Getting customers from {Route}", CustomersRoute);
            return await ReadJsonAsync<List<Customer>>(await _http.GetAsync(CustomersRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            throw;
        }
    }

    public async Task<Customer?> GetCustomerAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting customer {CustomerId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{CustomersRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Customer {CustomerId} not found", id);
                return null;
            }
            return await ReadJsonAsync<Customer>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", id);
            throw;
        }
    }

    public async Task<Customer> CreateCustomerAsync(Customer c)
    {
        try
        {
            _logger.LogInformation("Creating customer: {Username}", c.Username);
            return await ReadJsonAsync<Customer>(await _http.PostAsync(CustomersRoute, JsonBody(c)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer: {Username}", c.Username);
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
    {
        try
        {
            _logger.LogInformation("Updating customer {CustomerId}", id);
            var encodedId = Uri.EscapeDataString(id);
            return await ReadJsonAsync<Customer>(await _http.PutAsync($"{CustomersRoute}/{encodedId}", JsonBody(c)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            throw;
        }
    }

    public async Task DeleteCustomerAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting customer {CustomerId}", id);
            var encodedId = Uri.EscapeDataString(id);
            var response = await _http.DeleteAsync($"{CustomersRoute}/{encodedId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            throw;
        }
    }

    // ##################################################################
    // ---------- Customers (DTO-based methods) ----------
    // ##################################################################

    public async Task<List<CustomerDto>> GetCustomersDtoAsync()
    {
        try
        {
            _logger.LogInformation("Getting customers (DTO) from {Route}", CustomersRoute);
            return await ReadJsonAsync<List<CustomerDto>>(await _http.GetAsync(CustomersRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers (DTO)");
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerDtoAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting customer (DTO) {CustomerId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{CustomersRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Customer (DTO) {CustomerId} not found", id);
                return null;
            }
            return await ReadJsonAsync<CustomerDto>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer (DTO) {CustomerId}", id);
            throw;
        }
    }

    public async Task CreateCustomerAsync(CustomerDto customerDto)
    {
        try
        {
            _logger.LogInformation("Creating customer (DTO): {CustomerName}", customerDto.Name);
            var response = await _http.PostAsync(CustomersRoute, JsonBody(customerDto));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create customer (DTO). Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create customer (DTO). Status: {response.StatusCode}, Error: {errorContent}");
            }
            _logger.LogInformation("Customer (DTO) created successfully: {CustomerName}", customerDto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer (DTO): {CustomerName}", customerDto.Name);
            throw;
        }
    }

    public async Task UpdateCustomerAsync(string rowKey, CustomerDto customerDto)
    {
        try
        {
            _logger.LogInformation("Updating customer (DTO): {CustomerId}", rowKey);
            var encodedId = Uri.EscapeDataString(rowKey);

            var response = await _http.PutAsync($"{CustomersRoute}/{encodedId}", JsonBody(customerDto));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update customer (DTO). Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update customer (DTO). Status: {response.StatusCode}, Error: {errorContent}");
            }
            _logger.LogInformation("Customer (DTO) updated successfully: {CustomerId}", rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer (DTO): {CustomerId}", rowKey);
            throw;
        }
    }

    // ##################################################################
    // ---------- Products (Model-based methods) ----------
    // ##################################################################

    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            _logger.LogInformation("Getting products from {Route}", ProductsRoute);
            return await ReadJsonAsync<List<Product>>(await _http.GetAsync(ProductsRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            throw;
        }
    }

    public async Task<Product?> GetProductAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting product {ProductId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{ProductsRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found", id);
                return null;
            }
            return await ReadJsonAsync<Product>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            throw;
        }
    }

    public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
    {
        try
        {
            _logger.LogInformation("Creating product: {ProductName}", p.ProductName);
            return await SendProductFormAsync<Product>(
                p.ProductName,
                p.Description,
                (decimal)p.Price, // Cast double to decimal for price consistency
                p.StockAvailable,
                p.ImageUrl,
                imageFile,
                ProductsRoute,
                HttpMethod.Post
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductName}", p.ProductName);
            throw;
        }
    }

    public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
    {
        try
        {
            _logger.LogInformation("Updating product {ProductId}", id);
            var encodedId = Uri.EscapeDataString(id);
            return await SendProductFormAsync<Product>(
                p.ProductName,
                p.Description,
                (decimal)p.Price, // Cast double to decimal for price consistency
                p.StockAvailable,
                p.ImageUrl,
                imageFile,
                $"{ProductsRoute}/{encodedId}",
                HttpMethod.Put
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            throw;
        }
    }

    public async Task DeleteProductAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting product {ProductId}", id);
            var encodedId = Uri.EscapeDataString(id);
            var response = await _http.DeleteAsync($"{ProductsRoute}/{encodedId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            throw;
        }
    }

    // ##################################################################
    // ---------- Products (DTO-based methods) ----------
    // ##################################################################

    public async Task<List<ProductDto>> GetProductsDtoAsync()
    {
        try
        {
            _logger.LogInformation("Getting products (DTO) from {Route}", ProductsRoute);
            return await ReadJsonAsync<List<ProductDto>>(await _http.GetAsync(ProductsRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products (DTO)");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductDtoAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting product (DTO) {ProductId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{ProductsRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product (DTO) {ProductId} not found", id);
                return null;
            }
            return await ReadJsonAsync<ProductDto>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product (DTO) {ProductId}", id);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto productDto, IFormFile? imageFile)
    {
        try
        {
            _logger.LogInformation("Creating product (DTO): {ProductName}", productDto.ProductName);
            return await SendProductFormAsync<ProductDto>(productDto.ProductName, productDto.Description, productDto.Price, productDto.StockAvailable, productDto.ImageUrl, imageFile, ProductsRoute, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product (DTO): {ProductName}", productDto.ProductName);
            throw;
        }
    }

    public async Task<ProductDto> UpdateProductAsync(string rowKey, ProductDto productDto, IFormFile? imageFile)
    {
        try
        {
            _logger.LogInformation("Updating product (DTO): {ProductId}", rowKey);
            var encodedId = Uri.EscapeDataString(rowKey);
            return await SendProductFormAsync<ProductDto>(productDto.ProductName, productDto.Description, productDto.Price, productDto.StockAvailable, productDto.ImageUrl, imageFile, $"{ProductsRoute}/{encodedId}", HttpMethod.Put);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product (DTO): {ProductId}", rowKey);
            throw;
        }
    }

    // ##################################################################
    // ---------- Orders ----------
    // ##################################################################

    public async Task<List<Order>> GetOrdersAsync()
    {
        try
        {
            _logger.LogInformation("Getting orders from {Route}", OrdersRoute);
            return await ReadJsonAsync<List<Order>>(await _http.GetAsync(OrdersRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            throw;
        }
    }

    public async Task<Order?> GetOrderAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting order {OrderId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{OrdersRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return null;
            }
            return await ReadJsonAsync<Order>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            throw;
        }
    }

    public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
    {
        try
        {
            _logger.LogInformation("Creating order - Customer: {CustomerId}, Product: {ProductId}, Quantity: {Quantity}",
                customerId, productId, quantity);

            var payload = new { customerId, productId, quantity };
            var content = JsonBody(payload);

            var response = await _http.PostAsync(OrdersRoute, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Order creation failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Order creation failed: {response.StatusCode} - {errorContent}");
            }

            return await ReadJsonAsync<Order>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for Customer: {CustomerId}, Product: {ProductId}",
                customerId, productId);
            throw;
        }
    }

    public async Task UpdateOrderStatusAsync(string id, string newStatus)
    {
        try
        {
            _logger.LogInformation("Updating order {OrderId} status to {Status}", id, newStatus);
            var encodedId = Uri.EscapeDataString(id);

            var payload = new { status = newStatus };
            // Uses the custom PatchAsync extension
            var response = await _http.PatchAsync($"{OrdersRoute}/{encodedId}/status", JsonBody(payload));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Order status update failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Order status update failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status to {Status}", id, newStatus);
            throw;
        }
    }

    public async Task DeleteOrderAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting order {OrderId}", id);
            var encodedId = Uri.EscapeDataString(id);
            var response = await _http.DeleteAsync($"{OrdersRoute}/{encodedId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Order deletion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Order deletion failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            throw;
        }
    }

    // ##################################################################
    // ---------- Orders (DTO-based methods) ----------
    // ##################################################################

    public async Task<List<OrderDto>> GetOrdersDtoAsync()
    {
        try
        {
            _logger.LogInformation("Getting orders (DTO) from {Route}", OrdersRoute);
            return await ReadJsonAsync<List<OrderDto>>(await _http.GetAsync(OrdersRoute));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders (DTO)");
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderDtoAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting order (DTO) {OrderId}", id);
            var encodedId = Uri.EscapeDataString(id);

            var resp = await _http.GetAsync($"{OrdersRoute}/{encodedId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order (DTO) {OrderId} not found", id);
                return null;
            }
            return await ReadJsonAsync<OrderDto>(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order (DTO) {OrderId}", id);
            throw;
        }
    }

    // ##################################################################
    // ---------- Uploads ----------
    // ##################################################################

    public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
    {
        try
        {
            _logger.LogInformation("Uploading proof of payment - File: {FileName}, Order: {OrderId}",
                file.FileName, orderId);

            using var form = new MultipartFormDataContent();
            var sc = new StreamContent(file.OpenReadStream());
            sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            form.Add(sc, "ProofOfPayment", file.FileName);
            if (!string.IsNullOrWhiteSpace(orderId)) form.Add(new StringContent(orderId), "OrderId");
            if (!string.IsNullOrWhiteSpace(customerName)) form.Add(new StringContent(customerName), "CustomerName");

            var resp = await _http.PostAsync(UploadsRoute, form);

            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Upload failed: {StatusCode} - {Error}", resp.StatusCode, errorContent);
                throw new HttpRequestException($"Upload failed: {resp.StatusCode} - {errorContent}");
            }

            var doc = await ReadJsonAsync<Dictionary<string, string>>(resp);
            var fileName = doc.TryGetValue("fileName", out var name) ? name : file.FileName;

            _logger.LogInformation("Upload successful: {FileName}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading proof of payment");
            throw;
        }
    }
}

// ##################################################################
// ---------- HttpClient PATCH extension (Required for UpdateOrderStatusAsync) ----------
// ##################################################################

internal static class HttpClientPatchExtensions
{
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
    {
        return client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content });
    }
}