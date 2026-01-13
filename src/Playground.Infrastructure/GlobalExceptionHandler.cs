using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;

namespace Playground.Infrastructure;

internal sealed class GlobalExceptionHandler(
    IFeatureManager featureManager,
    ILogger<GlobalExceptionHandler> logger,
    ProblemDetailsFactory problemDetailsFactory,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceHeader = "X-Trace-Id";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred while processing the request.");

        var problemDetails = problemDetailsFactory.CreateProblemDetails(httpContext);

        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Title ??= "An unexpected error occurred.";

        var correlationId = GetCorrelationId(httpContext);
        var traceId = Activity.Current?.TraceId.ToHexString() ?? httpContext.TraceIdentifier;

        problemDetails.Detail = correlationId is not null
            ? "Please contact support with the provided correlation identifier."
            : "Please contact support with the provided trace identifier.";

        if (correlationId is not null)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
            httpContext.Response.Headers.TryAdd(CorrelationHeader, correlationId);
        }

        httpContext.Response.Headers.TryAdd(TraceHeader, traceId);

        if (await featureManager.IsEnabledAsync("IncludeExceptionDetailsInProblemDetails", cancellationToken))
        {
            problemDetails.Extensions["exception"] = new
            {
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace
            };
        }

        var statusCode = httpContext.Response.HasStarted
            ? httpContext.Response.StatusCode
            : StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = statusCode;
        problemDetails.Status = statusCode;

        problemDetails.Type ??= $"https://httpstatuses.com/{statusCode}";

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        };

        return await problemDetailsService.TryWriteAsync(context);
    }

    private static string? GetCorrelationId(HttpContext httpContext)
        => httpContext.Request.Headers.TryGetValue(CorrelationHeader, out var values) && !StringValues.IsNullOrEmpty(values)
            ? values.ToString()
            : null;
}