using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace ABCRetailsFunctions.Helpers;

/// <summary>
/// Helper class for parsing HTTP requests with 'multipart/form-data' content type, 
/// typically used for file uploads in Azure Functions.
/// </summary>
public static class MultipartHelper
{
    /// <summary>
    /// Represents a single file part uploaded in a multipart form.
    /// The stream <see cref="Data"/> is a <see cref="MemoryStream"/> created during parsing 
    /// and **must be disposed of by the consumer** of the <see cref="FormData"/>.
    /// </summary>
    public sealed record FilePart(string FieldName, string FileName, Stream Data);

    /// <summary>
    /// Represents the fully parsed multipart form data, containing both text fields and file parts.
    /// </summary>
    public sealed record FormData(IReadOnlyDictionary<string, string> Text, IReadOnlyList<FilePart> Files);

    /// <summary>
    /// Parses the entire request body stream as 'multipart/form-data'.
    /// </summary>
    /// <param name="body">The stream containing the multipart body content (e.g., HttpRequestData.Body).</param>
    /// <param name="contentType">The full 'Content-Type' header value, including the boundary.</param>
    /// <returns>A <see cref="FormData"/> object containing the parsed text fields and file parts.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the multipart boundary is missing or invalid.</exception>
    public static async Task<FormData> ParseAsync(Stream body, string contentType)
    {
        // 1. Extract the boundary parameter from the Content-Type header.
        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value
                           ?? throw new InvalidOperationException("Multipart boundary missing from Content-Type header.");

        var reader = new MultipartReader(boundary, body);
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<FilePart>();

        // 2. Read each section (part) of the multipart form.
        for (var section = await reader.ReadNextSectionAsync(); section != null; section = await reader.ReadNextSectionAsync())
        {
            if (section.ContentDisposition is null) continue; // Skip sections without content disposition

            var cd = ContentDispositionHeaderValue.Parse(section.ContentDisposition);

            // Check if it's a file upload
            if (cd.IsFileDisposition())
            {
                var fieldName = cd.Name.Value?.Trim('"') ?? "file";
                var fileName = cd.FileName.Value?.Trim('"') ?? "upload.bin";

                // Copy the file stream to a MemoryStream. This MemoryStream holds the entire file 
                // in memory and MUST be disposed by the function that consumes the FormData.
                var ms = new MemoryStream();
                await section.Body.CopyToAsync(ms);
                ms.Position = 0;

                files.Add(new FilePart(fieldName, fileName, ms));
            }
            // Check if it's a regular form text field
            else if (cd.IsFormDisposition())
            {
                var fieldName = cd.Name.Value?.Trim('"') ?? "";

                // Read the text content from the body section stream
                using var sr = new StreamReader(section.Body, Encoding.UTF8);
                text[fieldName] = await sr.ReadToEndAsync();
            }
        }

        // 3. Return the collected data.
        return new FormData(text, files);
    }
}
