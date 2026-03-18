using App.Core.Common;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using Newtonsoft.Json;

namespace App.API.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleException(context, ex);
            }
        }

        private static Task HandleException(HttpContext context, Exception ex)
        {
            var code = StatusCodes.Status500InternalServerError;
            var errors = new List<string> { ex.Message };

            switch (ex)
            {
                case EntityNotFoundException:
                    code = StatusCodes.Status404NotFound;
                    break;
                case Core.Exceptions.ValidationException validationEx:
                    code = StatusCodes.Status400BadRequest;
                    errors = validationEx.Errors.ToList();
                    break;
                case UnauthorizedException:
                    code = StatusCodes.Status401Unauthorized;
                    break;
                case ForbiddenException:
                    code = StatusCodes.Status403Forbidden;
                    break;
                case ConflictException:
                    code = StatusCodes.Status409Conflict;
                    break;
            }

            var result = JsonConvert.SerializeObject(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = errors,
                StatusCode = code
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;

            return context.Response.WriteAsync(result);
        }
    }
}
