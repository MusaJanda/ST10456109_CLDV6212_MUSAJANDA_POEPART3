using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ABCRetailsFunctions.Entities;
using ABCRetailsFunctions.Helpers;
using ABCRetailsFunctions.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net;

namespace ABCRetailsFunctions.Functions;

public class CustomerFunctions
{
    private readonly string _conn;
    private readonly string _table;
    private readonly ILogger<CustomerFunctions> _logger;

    public CustomerFunctions(IConfiguration cfg, ILogger<CustomerFunctions> logger)
    {
        _conn = cfg["AzureWebJobsStorage"]
                ?? cfg["ConnectionStrings:AzureStorage"]
                ?? throw new InvalidOperationException("AzureStorage connection string missing");

        _table = cfg["TABLE_CUSTOMER"] ?? "Customer";
        _logger = logger;

        _logger.LogInformation("✅ CustomerFunctions initialized with table: {Table}", _table);
    }

    [Function("Customers_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
    {
        _logger.LogInformation("📥 LIST CUSTOMERS - Request received from {RemoteIP}", GetClientIpAddress(req));

        try
        {
            var table = new TableClient(_conn, _table);
            await table.CreateIfNotExistsAsync();
            _logger.LogInformation("🔗 Connected to table: {TableName}", _table);

            var items = new List<CustomerDto>();
            var count = 0;

            await foreach (var e in table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
            {
                items.Add(Map.ToDto(e));
                count++;
            }

            _logger.LogInformation("📤 LIST CUSTOMERS - Returning {Count} customers", count);
            return await HttpJson.Ok(req, items); // FIXED: Added await
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LIST CUSTOMERS - Error retrieving customers: {ErrorMessage}", ex.Message);
            return await CreateJsonErrorResponseAsync(req, "Internal server error", System.Net.HttpStatusCode.InternalServerError);
        }


    }

    private async Task<HttpResponseData> CreateJsonErrorResponseAsync(HttpRequestData req, string v, HttpStatusCode internalServerError)
    {
        throw new NotImplementedException();
    }

    [Function("Customers_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 GET CUSTOMER - Request for ID: {CustomerId}", id);

        try
        {
            var table = new TableClient(_conn, _table);
            var resp = await table.GetEntityAsync<CustomerEntity>("Customer", id);

            _logger.LogInformation("✅ GET CUSTOMER - Found customer: {CustomerName} ({CustomerId})",
                resp.Value.Name, id);
            return await HttpJson.Ok(req, Map.ToDto(resp.Value)); // FIXED: Added await
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ GET CUSTOMER - Customer not found: {CustomerId}. Error: {ErrorMessage}", id, ex.Message);
            return await CreateJsonErrorResponseAsync(req, "Customer not found", System.Net.HttpStatusCode.NotFound);
        }
    }

    private sealed record CustomerCreateUpdate(
        string Name, string Surname, string Username, string Email, string ShippingAddress);

    [Function("Customers_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
    {
        _logger.LogInformation("📥 CREATE CUSTOMER - Request received");

        try
        {
            var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
            if (input is null)
            {
                _logger.LogWarning("⚠️ CREATE CUSTOMER - Invalid request body");
                return await HttpJson.BadRequest(req, "Invalid body"); // FIXED: Use HttpJson helper
            }

            _logger.LogInformation("👤 CREATE CUSTOMER - Creating customer: {Name} {Surname} ({Email})",
                input.Name, input.Surname, input.Email);

            var table = new TableClient(_conn, _table);
            await table.CreateIfNotExistsAsync();

            var customerId = Guid.NewGuid().ToString();
            var e = new CustomerEntity
            {
                PartitionKey = "Customer",
                RowKey = customerId,
                Name = input.Name,
                Surname = input.Surname,
                Username = input.Username,
                Email = input.Email,
                ShippingAddress = input.ShippingAddress
            };

            await table.AddEntityAsync(e);

            _logger.LogInformation("✅ CREATE CUSTOMER - Successfully created customer with ID: {CustomerId}", customerId);
            return await HttpJson.Created(req, Map.ToDto(e)); // FIXED: Added await
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CREATE CUSTOMER - Error creating customer: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Failed to create customer"); // FIXED: Use HttpJson helper
        }
    }

    [Function("Customers_Update")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 UPDATE CUSTOMER - Request for ID: {CustomerId}", id);

        try
        {
            var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
            if (input is null)
            {
                _logger.LogWarning("⚠️ UPDATE CUSTOMER - Invalid request body for customer: {CustomerId}", id);
                return await HttpJson.BadRequest(req, "Invalid body"); // FIXED: Use HttpJson helper
            }

            var table = new TableClient(_conn, _table);
            var resp = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            var e = resp.Value;

            _logger.LogInformation("✏️ UPDATE CUSTOMER - Updating customer: {OldName} -> {NewName}",
                e.Name, input.Name ?? e.Name);

            e.Name = input.Name ?? e.Name;
            e.Surname = input.Surname ?? e.Surname;
            e.Username = input.Username ?? e.Username;
            e.Email = input.Email ?? e.Email;
            e.ShippingAddress = input.ShippingAddress ?? e.ShippingAddress;

            await table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);

            _logger.LogInformation("✅ UPDATE CUSTOMER - Successfully updated customer: {CustomerId}", id);
            return await HttpJson.Ok(req, Map.ToDto(e)); // FIXED: Added await
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ UPDATE CUSTOMER - Customer not found or update failed: {CustomerId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Customer not found"); // FIXED: Use HttpJson helper
        }
    }

    [Function("Customers_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        _logger.LogInformation("📥 DELETE CUSTOMER - Request to delete ID: {CustomerId}", id);

        try
        {
            var table = new TableClient(_conn, _table);

            // First check if customer exists
            var existing = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            _logger.LogInformation("🗑️ DELETE CUSTOMER - Deleting customer: {CustomerName} ({CustomerId})",
                existing.Value.Name, id);

            await table.DeleteEntityAsync("Customer", id);

            _logger.LogInformation("✅ DELETE CUSTOMER - Successfully deleted customer: {CustomerId}", id);
            return HttpJson.NoContent(req); // FIXED: Use HttpJson helper
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ DELETE CUSTOMER - Customer not found or delete failed: {CustomerId}. Error: {ErrorMessage}",
                id, ex.Message);
            return await HttpJson.NotFound(req, "Customer not found"); // FIXED: Use HttpJson helper
        }
    }

    // REMOVED: Old error response methods since we're using HttpJson helpers now

    // Helper method to get client IP address for logging
    private static string GetClientIpAddress(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Forwarded-For", out var values))
            return values.FirstOrDefault() ?? "Unknown";

        return req.Url.Host;
    }
}