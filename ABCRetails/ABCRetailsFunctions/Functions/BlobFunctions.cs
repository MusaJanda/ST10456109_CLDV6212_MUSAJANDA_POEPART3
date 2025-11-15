using ABCRetailsFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.IO;

namespace ABCRetailsFunctions.Functions;

public class BlobFunctions
{
    private readonly ILogger<BlobFunctions> _logger;

    public BlobFunctions(ILogger<BlobFunctions> logger)
    {
        _logger = logger;
        _logger.LogInformation("✅ BlobFunctions initialized - Ready to process product image uploads");
    }
    
    [Function("OnProductImageUploaded")]
    public void OnProductImageUploaded(
        [BlobTrigger("%BLOB_PRODUCT_IMAGES%/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
        string name,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("OnProductImageUploaded");

        try
        {
            _logger.LogInformation("🖼️ BLOB TRIGGER - Product image uploaded: {FileName}", name);
            _logger.LogInformation("📊 BLOB TRIGGER - File size: {FileSize} bytes ({FileSizeKB} KB)",
                blob.Length, Math.Round(blob.Length / 1024.0, 2));

            // Log additional blob information
            _logger.LogInformation("📁 BLOB TRIGGER - Container: {ContainerName}",
                Environment.GetEnvironmentVariable("BLOB_PRODUCT_IMAGES") ?? "product-images");

            // Check if it's a valid image file
            var fileExtension = Path.GetExtension(name).ToLower();
            _logger.LogInformation("🔍 BLOB TRIGGER - File type: {FileExtension}", fileExtension);

            // Validate file type
            var validImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            if (!validImageTypes.Contains(fileExtension))
            {
                _logger.LogWarning("⚠️ BLOB TRIGGER - Non-image file uploaded: {FileName}", name);
            }
            else
            {
                _logger.LogInformation("✅ BLOB TRIGGER - Valid image file: {FileName}", name);
            }

            // Log processing completion
            _logger.LogInformation("🎉 BLOB TRIGGER - Successfully processed image upload: {FileName}", name);

            // Also log to the function context logger for redundancy
            log.LogInformation($"🖼️ Context Logger - Product image processed: {name}, size={blob.Length} bytes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ BLOB TRIGGER - Error processing uploaded image: {FileName}. Error: {ErrorMessage}",
                name, ex.Message);
            log.LogError($"❌ Context Logger - Error processing image {name}: {ex.Message}");

            // Re-throw to ensure the function fails and retries if configured
            throw;
        }
        finally
        {
            _logger.LogInformation("🏁 BLOB TRIGGER - Completed processing for: {FileName}", name);
        }
    }

    // Optional: Add a helper function to get image dimensions (if you want to log that)
    private void LogImageDetails(Stream blobStream, string fileName, ILogger logger)
    {
        try
        {
            // Note: Getting image dimensions requires image processing libraries
            // This is a placeholder for additional image analysis you might want to add
            _logger.LogInformation("🔍 BLOB TRIGGER - Image analysis placeholder for: {FileName}", fileName);

            // You could integrate with ImageSharp or System.Drawing here to get dimensions
            // var image = Image.Load(blobStream);
            // _logger.LogInformation("📐 BLOB TRIGGER - Image dimensions: {Width}x{Height}", image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ BLOB TRIGGER - Could not analyze image dimensions for {FileName}: {ErrorMessage}",
                fileName, ex.Message);
        }
    }
}