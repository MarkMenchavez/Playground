#pragma warning disable MA0048 // File name must match type name

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.AspNetCore;

namespace Playground.Infrastructure;

public interface IEchoServiceAgent
{
    Task<string> EchoAsync(string message);
}

public class EchoServiceAgent(HttpClient httpClient) : IEchoServiceAgent
{
    public async Task<string> EchoAsync(string message)
    {
        var content = new StringContent(message);

        var response = await httpClient.PostAsync("/api/echo", content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}

internal static class EchoEndpointBuilderExtensions
{
    public static IEndpointRouteBuilder MapEchoApi(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/echo", async (HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("EchoApi");

            using var reader = new StreamReader(httpContext.Request.Body);
            var message = await reader.ReadToEndAsync().ConfigureAwait(false);

            logger.LogInformation("Received echo message: {Message}", message);

            return Results.Ok(new { message });
        })
        .WithFeatureGate("EchoApi");

        return builder;
    }
}