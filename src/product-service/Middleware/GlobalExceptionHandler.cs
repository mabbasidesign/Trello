using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace product_service.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "An error occurred",
            status = 500,
            detail = exception.Message,
            instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails),
            cancellationToken);

        return true;
    }
}
