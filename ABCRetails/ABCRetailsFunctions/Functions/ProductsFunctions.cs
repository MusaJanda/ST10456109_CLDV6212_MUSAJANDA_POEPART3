using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ABCRetailsFunctions.Entities;
using ABCRetailsFunctions.Helpers;
using ABCRetailsFunctions.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;

namespace ABCRetailsFunctions.Functions;

public class ProductsFunctions
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string _images;
    private readonly ILogger<ProductsFunctions> _logger;

    public ProductsFunctions(IConfiguration cfg, ILogger<ProductsFunctions> logger)
    {
        _conn = cfg["AzureWebJobsStorage"]
                ?? cfg["ConnectionStrings:AzureStorage"]
                ?? throw new InvalidOperationException("AzureStorage connection string missing");

        _table = cfg["TABLE_PRODUCT"] ?? "Product";
        _images = cfg["BLOB_PRODUCT_IMAGES"] ?? "product-images";
        _logger = logger;

        _logger.LogInformation("✅ ProductsFunctions initialized - Table: {Table}, Image Container: {ImageContainer}",
            _table, _images);
    }

    [Function("Products_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
    {
        _logger.LogInformation("📥 LIST PRODUCTS - Request received");

        try
        {
            var table = new TableClient(_conn, _table);
            await table.CreateIfNotExistsAsync();
            _logger.LogInformation("🔗 Connected to products table: {TableName}", _table);

            var items = new List<ProductDto>();
            var count = 0;

            await foreach (var e in table.QueryAsync<ProductEntity>(x => x.PartitionKey == "Product"))
            {
                items.Add(Map.ToDto(e));
                count++;
            }

            _logger.LogInformation("📤 LIST PRODUCTS - Returning {Count} products", count);
            return await HttpJson.Ok(req, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LIST PRODUCTS - Error retrieving products: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Internal server error");
        }
    }

    [Function("Products_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 GET PRODUCT - Request for ID: {ProductId}", id);

        try
        {
            var table = new TableClient(_conn, _table);
            var resp = await table.GetEntityAsync<ProductEntity>("Product", id);

            _logger.LogInformation("✅ GET PRODUCT - Found product: {ProductName} (R{Price})",
                resp.Value.ProductName, resp.Value.Price);
            return await HttpJson.Ok(req, Map.ToDto(resp.Value));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ GET PRODUCT - Product not found: {ProductId}. Error: {ErrorMessage}", id, ex.Message);
            return await HttpJson.NotFound(req, "Product not found");
        }
    }

    private sealed record ProductCreateUpdate(string ProductName, string Description, decimal Price, int StockAvailable, string? ImageUrl);

    [Function("Products_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
    {
        _logger.LogInformation("📥 CREATE PRODUCT - Request received");

        try
        {
            var table = new TableClient(_conn, _table);
            var blobContainer = new BlobContainerClient(_conn, _images);
            await blobContainer.CreateIfNotExistsAsync();
            _logger.LogInformation("🔗 Connected to blob container: {ContainerName}", _images);

            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            _logger.LogInformation("📄 CREATE PRODUCT - Content type: {ContentType}", contentType);

            var productId = Guid.NewGuid().ToString("N");
            var productEntity = new ProductEntity(productId)
            {
                ImageUrl = "",
                Id = productId // ADDED: Set Id to match RowKey
            };

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🖼️ CREATE PRODUCT - Processing multipart form with image upload");

                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                var input = form.Text.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Populate entity from form text fields
                if (input.TryGetValue("ProductName", out var pn)) productEntity.ProductName = pn ?? string.Empty;
                if (input.TryGetValue("Description", out var d)) productEntity.Description = d ?? string.Empty;
                if (input.TryGetValue("Price", out var pr) && double.TryParse(pr, out var price)) productEntity.Price = price;
                if (input.TryGetValue("StockAvailable", out var st) && int.TryParse(st, out var stock)) productEntity.StockAvailable = stock;

                // Upload image if present
                if (file is not null && file.Data.Length > 0)
                {
                    var blobName = $"{productEntity.RowKey}-{file.FileName}";
                    var blob = blobContainer.GetBlobClient(blobName);
                    await using (var s = file.Data) await blob.UploadAsync(s, overwrite: true);
                    productEntity.ImageUrl = blob.Uri.ToString();
                    _logger.LogInformation("🖼️ CREATE PRODUCT - Image uploaded: {ImageUrl}", productEntity.ImageUrl);
                }
            }
            else
            {
                _logger.LogInformation("📋 CREATE PRODUCT - Processing JSON request");
                var input = await HttpJson.ReadAsync<ProductCreateUpdate>(req);
                if (input is null)
                {
                    _logger.LogWarning("⚠️ CREATE PRODUCT - Invalid JSON body");
                    return await HttpJson.BadRequest(req, "Invalid body");
                }

                productEntity.ProductName = input.ProductName ?? string.Empty;
                productEntity.Description = input.Description ?? string.Empty;
                productEntity.Price = Convert.ToDouble(input.Price);
                productEntity.StockAvailable = input.StockAvailable;
                productEntity.ImageUrl = input.ImageUrl ?? string.Empty;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(productEntity.ProductName))
            {
                _logger.LogWarning("⚠️ CREATE PRODUCT - Product name is required");
                return await HttpJson.BadRequest(req, "Product name is required");
            }

            if (productEntity.Price <= 0)
            {
                _logger.LogWarning("⚠️ CREATE PRODUCT - Price must be greater than 0");
                return await HttpJson.BadRequest(req, "Price must be greater than 0");
            }

            await table.AddEntityAsync(productEntity);
            _logger.LogInformation("✅ CREATE PRODUCT - Successfully created product: {ProductId} - {ProductName} (R{Price})",
                productId, productEntity.ProductName, productEntity.Price);

            return await HttpJson.Created(req, Map.ToDto(productEntity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CREATE PRODUCT - Error creating product: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Failed to create product");
        }
    }

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "products/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 UPDATE PRODUCT - Request for ID: {ProductId}", id);

        try
        {
            var table = new TableClient(_conn, _table);
            var resp = await table.GetEntityAsync<ProductEntity>("Product", id);
            var productEntity = resp.Value;

            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            _logger.LogInformation("📄 UPDATE PRODUCT - Content type: {ContentType}", contentType);

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                var input = form.Text.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Update entity from form text fields
                if (input.TryGetValue("ProductName", out var pn) && !string.IsNullOrEmpty(pn))
                    productEntity.ProductName = pn;
                if (input.TryGetValue("Description", out var d))
                    productEntity.Description = d ?? productEntity.Description;
                if (input.TryGetValue("Price", out var pr) && double.TryParse(pr, out var price) && price > 0)
                    productEntity.Price = price;
                if (input.TryGetValue("StockAvailable", out var st) && int.TryParse(st, out var stock))
                    productEntity.StockAvailable = stock;

                // Handle image upload/replacement
                if (file is not null && file.Data.Length > 0)
                {
                    var blobContainer = new BlobContainerClient(_conn, _images);
                    await blobContainer.CreateIfNotExistsAsync();
                    var blobName = $"{productEntity.RowKey}-{file.FileName}";
                    var blob = blobContainer.GetBlobClient(blobName);
                    await using (var s = file.Data) await blob.UploadAsync(s, overwrite: true);
                    productEntity.ImageUrl = blob.Uri.ToString();
                    _logger.LogInformation("🖼️ UPDATE PRODUCT - Image updated: {ImageUrl}", productEntity.ImageUrl);
                }
            }
            else
            {
                var input = await HttpJson.ReadAsync<ProductCreateUpdate>(req);
                if (input is null)
                {
                    _logger.LogWarning("⚠️ UPDATE PRODUCT - Invalid JSON body");
                    return await HttpJson.BadRequest(req, "Invalid body");
                }

                // Update entity from JSON
                if (!string.IsNullOrEmpty(input.ProductName))
                    productEntity.ProductName = input.ProductName;

                if (!string.IsNullOrEmpty(input.Description))
                    productEntity.Description = input.Description;

                if (input.Price > 0)
                    productEntity.Price = Convert.ToDouble(input.Price);

                productEntity.StockAvailable = input.StockAvailable;

                if (!string.IsNullOrEmpty(input.ImageUrl))
                    productEntity.ImageUrl = input.ImageUrl;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(productEntity.ProductName))
            {
                _logger.LogWarning("⚠️ UPDATE PRODUCT - Product name cannot be empty");
                return await HttpJson.BadRequest(req, "Product name cannot be empty");
            }

            if (productEntity.Price <= 0)
            {
                _logger.LogWarning("⚠️ UPDATE PRODUCT - Price must be greater than 0");
                return await HttpJson.BadRequest(req, "Price must be greater than 0");
            }

            await table.UpdateEntityAsync(productEntity, productEntity.ETag, TableUpdateMode.Replace);
            _logger.LogInformation("✅ UPDATE PRODUCT - Successfully updated product: {ProductId} - {ProductName}",
                id, productEntity.ProductName);

            return await HttpJson.Ok(req, Map.ToDto(productEntity));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ UPDATE PRODUCT - Product not found or update failed: {ProductId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Product not found");
        }
    }

    [Function("Products_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "products/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 DELETE PRODUCT - Request to delete ID: {ProductId}", id);

        try
        {
            var table = new TableClient(_conn, _table);
            var existing = await table.GetEntityAsync<ProductEntity>("Product", id);

            // Delete associated image if exists
            if (!string.IsNullOrEmpty(existing.Value.ImageUrl))
            {
                try
                {
                    var blobContainer = new BlobContainerClient(_conn, _images);
                    var blobName = existing.Value.ImageUrl.Split('/').Last();
                    var blob = blobContainer.GetBlobClient(blobName);
                    await blob.DeleteIfExistsAsync();
                    _logger.LogInformation("🗑️ DELETE PRODUCT - Deleted associated image: {ImageUrl}", existing.Value.ImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ DELETE PRODUCT - Failed to delete image, continuing with product deletion: {ErrorMessage}", ex.Message);
                }
            }

            await table.DeleteEntityAsync("Product", id);

            _logger.LogInformation("✅ DELETE PRODUCT - Successfully deleted product: {ProductId} - {ProductName}",
                id, existing.Value.ProductName);
            return HttpJson.NoContent(req);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ DELETE PRODUCT - Product not found or delete failed: {ProductId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Product not found");
        }
    }
}