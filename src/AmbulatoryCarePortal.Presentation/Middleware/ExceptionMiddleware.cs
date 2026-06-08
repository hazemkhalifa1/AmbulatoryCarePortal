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
            _logger.LogError($"An unhandled exception occurred: {exception.Message}", exception);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ErrorResponse
        {
            Message = exception.Message,
            StatusCode = context.Response.StatusCode
        };

        if (exception is ArgumentException || exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.StatusCode = context.Response.StatusCode;
            response.Message = "Invalid request parameters";
        }
        else if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.StatusCode = context.Response.StatusCode;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
}
