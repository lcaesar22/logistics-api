using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Common.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;  // Client closes the request
            return true;
        }

        var (status, title) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request could not be processed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
        
        logger.Log(
            status >= 500 ? LogLevel.Error : LogLevel.Warning, exception,
            "{RequestMethod} {RequestPath} failed with {StatusCode}", httpContext.Request.Method, httpContext.Request.Path, status);
        
        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = environment.IsDevelopment() ? exception.Message : null
            }
        });
    }
}