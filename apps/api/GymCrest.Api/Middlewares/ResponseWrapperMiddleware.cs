using GymCrest.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymCrest.Api.Middlewares
{
    public class ResponseWrapperMiddleware
    {

        private readonly RequestDelegate _next;

        public ResponseWrapperMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip for swagger and export routes
            string path = context.Request.Path.Value ?? "";
            if (path.Contains("/swagger") || path.Contains("/export") || path.Contains("/index.html"))
            {
                await _next(context);
                return;
            }

            // Capture the original body stream
            Stream originalBodyStream = context.Response.Body;
            using MemoryStream newBodyStream = new MemoryStream();
            context.Response.Body = newBodyStream;

            try
            {
                await _next(context);

                // If it's not a successful JSON response, just copy it back
                if (context.Response.StatusCode >= 400 || 
                    context.Response.ContentType?.Contains("application/json") == false)
                {
                    newBodyStream.Seek(0, SeekOrigin.Begin);
                    await newBodyStream.CopyToAsync(originalBodyStream);
                    return;
                }

                // Read the response body
                newBodyStream.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(newBodyStream).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(responseBody))
                {
                    newBodyStream.Seek(0, SeekOrigin.Begin);
                    await newBodyStream.CopyToAsync(originalBodyStream);
                    return;
                }

                // Check if it's already wrapped to avoid double wrapping
                if (responseBody.Contains("\"success\":") && responseBody.Contains("\"data\":"))
                {
                    newBodyStream.Seek(0, SeekOrigin.Begin);
                    await newBodyStream.CopyToAsync(originalBodyStream);
                    return;
                }

                // Parse the data
                object? data;
                try {
                    data = JsonSerializer.Deserialize<JsonElement>(responseBody);
                } catch {
                    data = responseBody;
                }

                var wrappedResponse = new ApiResponse<object>
                {
                    Success = true,
                    StatusCode = context.Response.StatusCode,
                    Data = data,
                    Message = "Request successful",
                    Timestamp = DateTime.UtcNow
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(wrappedResponse, options);

                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = null;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(json);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
