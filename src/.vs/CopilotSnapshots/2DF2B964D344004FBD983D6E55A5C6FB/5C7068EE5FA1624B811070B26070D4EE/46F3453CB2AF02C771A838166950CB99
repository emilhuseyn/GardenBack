using System.Text.RegularExpressions;

namespace App.API.Middlewares
{
    public class XSSProtectionMiddleware
    {
        private readonly RequestDelegate _next;

        public XSSProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post ||
                context.Request.Method == HttpMethods.Put ||
                context.Request.Method == HttpMethods.Patch)
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                var xssPattern = new Regex(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase);

                if (xssPattern.IsMatch(body))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Potential XSS detected. The request was blocked.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
