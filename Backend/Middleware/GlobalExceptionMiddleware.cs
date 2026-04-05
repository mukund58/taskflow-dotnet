namespace Backend.Middleware;

using System.Net;
using System.Text.Json;
using Backend.Models.DTOs;
using FluentValidation;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationException =>
                ((int)HttpStatusCode.BadRequest,
                 "Validation failed",
                 validationException.Errors
                     .GroupBy(e => e.PropertyName)
                     .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray())),

            KeyNotFoundException =>
                ((int)HttpStatusCode.NotFound,
                 exception.Message,
                 (object?)null),

            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Unauthorized,
                 "Unauthorized",
                 (object?)null),

            _ =>
                ((int)HttpStatusCode.InternalServerError,
                 "An unexpected error occurred",
                 (object?)null)
        };

        context.Response.StatusCode = statusCode;

        var response = ApiResponseDto<object>.Fail(message, errors);
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}