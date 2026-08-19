using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Middlewares;

public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationProblem = new ValidationProblemDetails(validationException.Errors)
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = validationException.Message,
                    Instance = context.Request.Path
                };
                validationProblem.Extensions["traceId"] = traceId;
                problemDetails = validationProblem;
                break;

            case KeyNotFoundException keyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Resource not found.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Detail = keyNotFoundException.Message,
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = traceId;
                break;

            case UnauthorizedAccessException unauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = "Forbidden.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Detail = unauthorizedException.Message,
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = traceId;
                break;

            case InvalidOperationException invalidOpException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid operation.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = invalidOpException.Message,
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = traceId;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = _env.IsDevelopment() ? exception.Message : "An unexpected internal server error occurred.",
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = traceId;
                break;
        }

        var json = JsonSerializer.Serialize((object)problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return context.Response.WriteAsync(json);
    }
}
