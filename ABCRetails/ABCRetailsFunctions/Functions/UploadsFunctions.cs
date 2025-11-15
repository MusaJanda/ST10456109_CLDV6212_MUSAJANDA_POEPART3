using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABCRetailsFunctions.Helpers;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailsFunctions.Functions;

public class UploadsFunctions
{
    private readonly string _conn;
    private readonly string _proofs;
    private readonly string _share;
    private readonly string _shareDir;
    private readonly ILogger<UploadsFunctions> _logger;

    public UploadsFunctions(IConfiguration cfg, ILogger<UploadsFunctions> logger)
    {
        _conn = cfg["AzureWebJobsStorage"]
                ?? cfg["ConnectionStrings:AzureStorage"]
                ?? throw new InvalidOperationException("AzureStorage connection string missing");

        _proofs = cfg["BLOB_PAYMENT_PROOFS"] ?? "payment-proofs";
        _share = cfg["FILESHARE_CONTRACTS"] ?? "contracts";
        _shareDir = cfg["FILESHARE_DIR_PAYMENTS"] ?? "payments";
        _logger = logger;

        _logger.LogInformation("✅ UploadsFunctions initialized - Blob: {ProofsContainer}, FileShare: {FileShare}, Directory: {ShareDir}",
            _proofs, _share, _shareDir);
    }

    [Function("Uploads_ProofOfPayment")]
    public async Task<HttpResponseData> Proof(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uploads/proof-of-payment")] HttpRequestData req)
    {
        _logger.LogInformation("📥 UPLOAD PROOF OF PAYMENT - Request received");

        try
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
            _logger.LogInformation("📄 UPLOAD PROOF OF PAYMENT - Content type: {ContentType}", contentType);

            if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️ UPLOAD PROOF OF PAYMENT - Expected multipart/form-data, got: {ContentType}", contentType);
                return await HttpJson.BadRequest(req, "Expected multipart/form-data"); // FIXED: Use HttpJson helper
            }

            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            var file = form.Files.FirstOrDefault(f => f.FieldName == "ProofOfPayment");

            if (file is null || file.Data.Length == 0)
            {
                _logger.LogWarning("⚠️ UPLOAD PROOF OF PAYMENT - ProofOfPayment file is required but not provided");
                return await HttpJson.BadRequest(req, "ProofOfPayment file is required"); // FIXED: Use HttpJson helper
            }

            var orderId = form.Text.GetValueOrDefault("OrderId") ?? "Unknown";
            var customerName = form.Text.GetValueOrDefault("CustomerName") ?? "Unknown";

            _logger.LogInformation("📁 UPLOAD PROOF OF PAYMENT - Processing file: {FileName} for Order: {OrderId}, Customer: {CustomerName}",
                file.FileName, orderId, customerName);

            // Blob Storage Upload
            var container = new BlobContainerClient(_conn, _proofs);
            await container.CreateIfNotExistsAsync();
            _logger.LogInformation("🔗 UPLOAD PROOF OF PAYMENT - Connected to blob container: {ContainerName}", _proofs);

            var blobName = $"{Guid.NewGuid():N}-{SanitizeFileName(file.FileName)}";
            var blob = container.GetBlobClient(blobName);

            // Set blob metadata
            var metadata = new Dictionary<string, string>
            {
                ["OrderId"] = orderId,
                ["CustomerName"] = customerName,
                ["UploadedAtUtc"] = DateTimeOffset.UtcNow.ToString("O"),
                ["OriginalFileName"] = file.FileName
            };

            await using (var stream = file.Data)
            {
                await blob.UploadAsync(stream, new BlobUploadOptions
                {
                    Metadata = metadata,
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = GetContentType(file.FileName)
                    }
                });
            }

            _logger.LogInformation("✅ UPLOAD PROOF OF PAYMENT - File uploaded to blob: {BlobName}, Size: {FileSize} bytes",
                blobName, file.Data.Length);

            // Azure File Share Metadata Upload (for additional backup/reference)
            try
            {
                var share = new ShareClient(_conn, _share);
                await share.CreateIfNotExistsAsync();
                var root = share.GetRootDirectoryClient();
                var dir = root.GetSubdirectoryClient(_shareDir);
                await dir.CreateIfNotExistsAsync();

                var fileClient = dir.GetFileClient($"{blobName}.meta.txt");
                var metaContent = $"UploadedAtUtc: {DateTimeOffset.UtcNow:O}\n" +
                                $"OrderId: {orderId}\n" +
                                $"CustomerName: {customerName}\n" +
                                $"BlobUrl: {blob.Uri}\n" +
                                $"OriginalFileName: {file.FileName}\n" +
                                $"FileSize: {file.Data.Length} bytes\n" +
                                $"ContentType: {GetContentType(file.FileName)}";

                var bytes = Encoding.UTF8.GetBytes(metaContent);
                using var ms = new MemoryStream(bytes);
                await fileClient.CreateAsync(ms.Length);
                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, ms.Length), ms);

                _logger.LogInformation("✅ UPLOAD PROOF OF PAYMENT - Metadata saved to file share: {FileName}", $"{blobName}.meta.txt");
            }
            catch (Exception ex)
            {
                // Log but don't fail the upload if file share operations fail
                _logger.LogWarning(ex, "⚠️ UPLOAD PROOF OF PAYMENT - Failed to save metadata to file share, continuing with blob upload");
            }

            var responseData = new
            {
                fileName = blobName,
                url = blob.Uri.ToString(),
                orderId = orderId,
                customerName = customerName,
                fileSize = file.Data.Length,
                uploadedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("✅ UPLOAD PROOF OF PAYMENT - Upload completed successfully for Order: {OrderId}", orderId);
            return await HttpJson.Ok(req, responseData); // FIXED: Added await
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UPLOAD PROOF OF PAYMENT - Error uploading proof of payment: {ErrorMessage}", ex.Message);
            return await HttpJson.InternalServerError(req, "Failed to upload proof of payment"); // FIXED: Use HttpJson helper
        }
    }

    /// <summary>
    /// Sanitizes file names to remove potentially dangerous characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "file";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());

        return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
    }

    /// <summary>
    /// Gets the content type based on file extension
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    // REMOVED: CreateErrorResponseAsync method since we're using HttpJson helpers
}