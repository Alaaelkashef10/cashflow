using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Errors;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Client mistakes such as a missing or malformed required header or body.
        if (exception is not BadHttpRequestException bad)
            return false;

        httpContext.Response.StatusCode = bad.StatusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = bad.StatusCode,
                Title = "Bad request",
                Detail = bad.Message
            }
        });
    }
}