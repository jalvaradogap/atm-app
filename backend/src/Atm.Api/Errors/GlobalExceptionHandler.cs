using Atm.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atm.Api.Errors;

/// <summary>
/// The one place where exceptions become HTTP responses (RFC 7807 problem+json). Domain messages are
/// written for the user, so they are returned. Anything unexpected is logged and returned generically.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
            },
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        AccountNotFoundException e => (StatusCodes.Status404NotFound, "Account not found", e.Message),
        DomainException e => (StatusCodes.Status422UnprocessableEntity, "The request cannot be processed", e.Message),
        DbUpdateConcurrencyException => (
            StatusCodes.Status409Conflict,
            "Conflict",
            "The account was changed by another request. Reload it and try again."),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "Something went wrong. Please try again later."),
    };
}
