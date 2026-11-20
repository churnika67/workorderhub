using Microsoft.AspNetCore.Mvc;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Middleware;

/// <summary>
/// Turns known domain errors into RFC 7807 problem responses and logs anything unexpected.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (InvalidTransitionException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Invalid status change", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Request not allowed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Server error",
                "Something went wrong. The error has been logged.");
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        }, options: null, contentType: "application/problem+json");
    }
}
