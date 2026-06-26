using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string message;
        if (exception is ArgumentException || exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            message = "Invalid request parameters";
        }
        else if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            message = "Unauthorized";
        }
        else
        {
            message = "An unexpected error occurred. Please try again later.";
        }

        var response = new ErrorResponse
        {
            Message = message,
            StatusCode = context.Response.StatusCode
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
}
