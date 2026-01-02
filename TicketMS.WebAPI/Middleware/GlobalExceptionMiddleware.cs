using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using TicketMS.Application.Common;
using TicketMS.WebAPI.Middleware.Models;

namespace TicketMS.WebAPI.Middleware
{
    public class GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;
        private readonly IHostEnvironment _env = env;

        // Track errors per IP/User
        private static readonly ConcurrentDictionary<string, ErrorTracker> _errorTrackers = new();

        // Configuration
        private const int MaxErrorsAllowed = 3;
        private static readonly TimeSpan ErrorWindowDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(15);

        public async Task InvokeAsync(HttpContext context)
        {
            var clientKey = GetClientKey(context);

            // Check if client is in cooldown period
            if (IsInCooldown(clientKey))
            {
                await HandleTooManyErrorsAsync(context, clientKey);
                return;
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Track the error
                TrackError(clientKey);

                // Check if max errors exceeded
                if (HasExceededMaxErrors(clientKey))
                {
                    await HandleTooManyErrorsAsync(context, clientKey);
                    return;
                }

                await HandleExceptionAsync(context, ex, clientKey);
            }
        }

        private static string GetClientKey(HttpContext context)
        {
            // Use User ID if authenticated, otherwise use IP
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value
                : null;

            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip_{ipAddress}";
        }

        private static void TrackError(string clientKey)
        {
            var tracker = _errorTrackers.GetOrAdd(clientKey, _ => new ErrorTracker());
            tracker.AddError();
        }

        private static bool HasExceededMaxErrors(string clientKey)
        {
            if (_errorTrackers.TryGetValue(clientKey, out var tracker))
            {
                return tracker.GetErrorCount(ErrorWindowDuration) >= MaxErrorsAllowed;
            }
            return false;
        }

        private static bool IsInCooldown(string clientKey)
        {
            if (_errorTrackers.TryGetValue(clientKey, out var tracker))
            {
                return tracker.IsInCooldown(CooldownDuration);
            }
            return false;
        }

        private async Task HandleTooManyErrorsAsync(HttpContext context, string clientKey)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";

            _logger.LogWarning(
                "[{CorrelationId}] Client {ClientKey} has exceeded max error attempts. Blocked temporarily.",
                correlationId,
                clientKey
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;

            // Calculate remaining cooldown time
            var remainingTime = GetRemainingCooldownTime(clientKey);

            var response = new ApiResponse
            {
                Success = false,
                Message = "Too many errors occurred. Please try again later or contact admin.",
                Errors = new List<string>
                {
                    $"Please wait {remainingTime.Minutes} minutes before trying again.",
                    "If the issue persists, contact support at: support@yourcompany.com"
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        private static TimeSpan GetRemainingCooldownTime(string clientKey)
        {
            if (_errorTrackers.TryGetValue(clientKey, out var tracker))
            {
                return tracker.GetRemainingCooldown(CooldownDuration);
            }
            return TimeSpan.Zero;
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string clientKey)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            var errorCount = _errorTrackers.TryGetValue(clientKey, out var tracker)
                ? tracker.GetErrorCount(ErrorWindowDuration)
                : 1;

            _logger.LogError(
                exception,
                "[{CorrelationId}] Unhandled exception occurred (Attempt {ErrorCount}/{MaxErrors}): {Message}",
                correlationId,
                errorCount,
                MaxErrorsAllowed,
                exception.Message
            );

            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };

            context.Response.StatusCode = (int)statusCode;

            // Add warning if approaching max errors
            var warningMessage = errorCount >= MaxErrorsAllowed - 1
                ? $"Warning: You have {MaxErrorsAllowed - errorCount} attempt(s) remaining before temporary block."
                : null;

            var errors = new List<string>();

            if (_env.IsDevelopment())
            {
                errors.Add(exception.Message);
                errors.Add(exception.StackTrace ?? "");
            }
            else
            {
                errors.Add("Please contact support if the problem persists.");
            }

            if (!string.IsNullOrEmpty(warningMessage))
            {
                errors.Insert(0, warningMessage);
            }

            var response = new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        // Cleanup old trackers periodically (call this from a background service)
        public static void CleanupOldTrackers()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            var keysToRemove = _errorTrackers
                .Where(kvp => kvp.Value.LastErrorTime < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _errorTrackers.TryRemove(key, out _);
            }
        }
    }
}