using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.Exceptions;
using WeeklyPlanning.Domain.Exceptions;

namespace WeeklyPlanning.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, "Recurso no encontrado", ex.Message, (IEnumerable<string>?)null),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflicto", ex.Message, null),
            ValidationException ex => (StatusCodes.Status422UnprocessableEntity, "Error de validación", ex.Message, ex.Errors),
            DomainException ex => (StatusCodes.Status400BadRequest, "Regla de negocio violada", ex.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor", "Ocurrió un error inesperado.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Handled exception [{StatusCode}]: {Message}", statusCode, exception.Message);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
