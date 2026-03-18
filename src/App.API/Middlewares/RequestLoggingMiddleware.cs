using System.Diagnostics;
using System.Security.Claims;

namespace App.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms [User: {UserId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId);
        }
    }
}
