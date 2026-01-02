using System.Diagnostics;
using System.Text;

namespace TicketMS.WebAPI.Middleware
{
    public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            var stopwatch = Stopwatch.StartNew();

            // Get client IP address
            var ipAddress = GetClientIpAddress(context);

            // Log request
            var requestLog = await GetRequestLog(context);
            _logger.LogInformation(
                "[{CorrelationId}] HTTP {Method} {Path} | IP: {IpAddress} | Request: {Request}",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                ipAddress,
                requestLog
            );

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                // Log response
                var responseLog = await GetResponseLog(context);
                _logger.LogInformation(
                    "[{CorrelationId}] HTTP {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | Response: {Response}",
                    correlationId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseLog
                );

                // Copy response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP (behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            // Check for real IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private static async Task<string> GetRequestLog(HttpContext context)
        {
            // Skip logging for GET requests or if no body
            if (context.Request.Method == "GET" || context.Request.ContentLength == null || context.Request.ContentLength == 0)
            {
                return "No body";
            }

            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true
            );

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Sanitize sensitive data
            body = SanitizeLog(body);

            // Truncate if too long
            if (body.Length > 500)
            {
                body = body[..500] + "...[truncated]";
            }

            return body;
        }

        private static async Task<string> GetResponseLog(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Truncate if too long
            if (body.Length > 500)
            {
                body = body[..500] + "...[truncated]";
            }

            return body;
        }

        private static string SanitizeLog(string body)
        {
            // Remove passwords from logs
            var sensitiveFields = new[] { "password", "confirmPassword", "currentPassword", "newPassword", "token", "refreshToken" };

            foreach (var field in sensitiveFields)
            {
                // Simple regex-free replacement for JSON
                var patterns = new[]
                {
                $"\"{field}\":\"{GetValuePattern()}\"",
                $"\"{field}\": \"{GetValuePattern()}\""
            };

                body = System.Text.RegularExpressions.Regex.Replace(
                    body,
                    $"(\"{field}\"\\s*:\\s*\")[^\"]*\"",
                    $"$1***REDACTED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }

            return body;
        }

        private static string GetValuePattern() => "[^\"]*";
    }
}
