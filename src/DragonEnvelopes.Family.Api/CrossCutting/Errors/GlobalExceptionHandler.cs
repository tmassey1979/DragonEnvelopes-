using System.Text.Json;
using DragonEnvelopes.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DragonEnvelopes.Family.Api.CrossCutting.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(validationException),
            DomainValidationException domainValidationException => CreateProblemDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Domain validation failed.",
                domainValidationException.Message),
            BadHttpRequestException badHttpRequestException => CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid request payload.",
                badHttpRequestException.Message),
            JsonException jsonException => CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid JSON payload.",
                jsonException.Message),
            _ => CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "Internal server error.",
                "An unexpected error occurred.")
        };

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing request. {ExceptionType}", exception.GetType().Name);
        }
        else
        {
            logger.LogWarning(exception, "Handled exception while processing request. {ExceptionType}", exception.GetType().Name);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
    }

    private static HttpValidationProblemDetails CreateValidationProblemDetails(ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(static failure =>
                string.IsNullOrWhiteSpace(failure.PropertyName) ? "request" : failure.PropertyName)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(failure => failure.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed.",
            Detail = "One or more validation errors occurred."
        };
    }
}
