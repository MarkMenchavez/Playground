using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;

namespace Playground.Infrastructure;

internal static partial class GlobalExceptionHandlerLogger
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred while processing the request.")]
    public static partial void UnhandledException(
        this ILogger<GlobalExceptionHandler> logger,
        Exception exception);
}

internal sealed class GlobalExceptionHandler(
    IFeatureManager featureManager,
    ILogger<GlobalExceptionHandler> logger,
    ProblemDetailsFactory problemDetailsFactory,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceHeader = "X-Trace-Id";
    private const string IncludeExceptionDetailsInProblemDetailsFeature = "IncludeExceptionDetailsInProblemDetails";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.UnhandledException(exception);

        var problemDetails = problemDetailsFactory.CreateProblemDetails(httpContext);

        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Title ??= "An unexpected error occurred.";

        var correlationId = GetCorrelationId(httpContext);
        var traceId = Activity.Current?.TraceId.ToHexString() ?? httpContext.TraceIdentifier;

        problemDetails.Detail = !string.IsNullOrWhiteSpace(correlationId)
            ? "Please contact support with the provided correlation identifier."
            : "Please contact support with the provided trace identifier.";

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
            httpContext.Response.Headers.TryAdd(CorrelationHeader, correlationId);
        }

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.Headers.TryAdd(TraceHeader, traceId);

        if (await featureManager.IsEnabledAsync(IncludeExceptionDetailsInProblemDetailsFeature, cancellationToken)
            .ConfigureAwait(false))
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

        return await problemDetailsService.TryWriteAsync(context).ConfigureAwait(false);
    }

    private static string? GetCorrelationId(HttpContext httpContext)
        => httpContext.Request.Headers.TryGetValue(CorrelationHeader, out var values) && !StringValues.IsNullOrEmpty(values)
            ? values.ToString()
            : null;
}