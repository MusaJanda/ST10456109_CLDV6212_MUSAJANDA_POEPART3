using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ABCRetailsFunctions.Helpers;

public static class HttpJson
{
    // Standard JSON options for web APIs (camelCase, case-insensitive reading)
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Reads the request body stream and attempts to deserialize it as JSON into type T.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the request body into.</typeparam>
    /// <param name="req">The HTTP request data.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public static async Task<T?> ReadAsync<T>(HttpRequestData req)
    {
        try
        {
            using var reader = new StreamReader(req.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, _json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Creates an HTTP 200 OK response with a JSON body.
    /// </summary>
    public static async Task<HttpResponseData> Ok<T>(HttpRequestData req, T body)
        => await WriteAsync(req, HttpStatusCode.OK, body);

    /// <summary>
    /// Creates an HTTP 201 Created response with a JSON body.
    /// </summary>
    public static async Task<HttpResponseData> Created<T>(HttpRequestData req, T body)
        => await WriteAsync(req, HttpStatusCode.Created, body);

    /// <summary>
    /// Creates an HTTP 400 Bad Request response with a JSON error message.
    /// </summary>
    public static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        => await WriteAsync(req, HttpStatusCode.BadRequest, new { error = message });

    /// <summary>
    /// Creates an HTTP 404 Not Found response with a JSON error message.
    /// </summary>
    public static async Task<HttpResponseData> NotFound(HttpRequestData req, string message = "Not Found")
        => await WriteAsync(req, HttpStatusCode.NotFound, new { error = message });

    /// <summary>
    /// Creates an HTTP 500 Internal Server Error response with a JSON error message.
    /// </summary>
    public static async Task<HttpResponseData> InternalServerError(HttpRequestData req, string message = "An internal server error occurred")
        => await WriteAsync(req, HttpStatusCode.InternalServerError, new { error = message });

    /// <summary>
    /// Creates an HTTP 204 No Content response.
    /// </summary>
    public static HttpResponseData NoContent(HttpRequestData req)
        => req.CreateResponse(HttpStatusCode.NoContent);

    /// <summary>
    /// Creates a response with a plain text body and specified status code.
    /// </summary>
    public static async Task<HttpResponseData> TextAsync(HttpRequestData req, HttpStatusCode code, string message)
    {
        var response = req.CreateResponse(code);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(message, Encoding.UTF8);
        return response;
    }

    /// <summary>
    /// Private helper to serialize a body object to JSON and write the response stream asynchronously.
    /// </summary>
    private static async Task<HttpResponseData> WriteAsync<T>(HttpRequestData req, HttpStatusCode code, T body)
    {
        var response = req.CreateResponse(code);

        // Set content type only if not already set
        if (!response.Headers.Contains("Content-Type"))
        {
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        }

        var json = JsonSerializer.Serialize(body, _json);
        await response.WriteStringAsync(json, Encoding.UTF8);

        return response;
    }

    /// <summary>
    /// Creates a response with custom status code and JSON body.
    /// </summary>
    public static async Task<HttpResponseData> CreateResponse<T>(HttpRequestData req, HttpStatusCode code, T body)
        => await WriteAsync(req, code, body);

    /// <summary>
    /// Creates a simple success response with message.
    /// </summary>
    public static async Task<HttpResponseData> Success(HttpRequestData req, string message)
        => await WriteAsync(req, HttpStatusCode.OK, new { message = message });
}