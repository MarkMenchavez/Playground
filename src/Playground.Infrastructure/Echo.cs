#pragma warning disable MA0048 // File name must match type name

using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.AspNetCore;

namespace Playground.Infrastructure;

public interface IEchoServiceAgent
{
    Task<string> EchoAsync(string message, CancellationToken cancellationToken = default);
}

public class EchoServiceAgent(HttpClient httpClient) : IEchoServiceAgent
{
    public async Task<string> EchoAsync(string message, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(message, Encoding.UTF8, "text/plain");

        var response = await httpClient.PostAsync("/api/echo", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class EchoEndpointBuilderExtensions
{
    private const string EchoApiFeature = "EchoApi";

    public static IEndpointRouteBuilder MapEchoApi(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/echo", async (HttpContext context, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("EchoApi");

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var message = await reader.ReadToEndAsync(cancellationToken);

            // Sanitize user input before logging to prevent log forging via control characters.
            var sanitizedMessage = new string(message.Where(c => !char.IsControl(c) || c == '\t').ToArray());
            logger.LogInformation("Received echo message: {Message}", sanitizedMessage);

            return Results.Text(message, "text/plain", Encoding.UTF8);
        })
        .WithFeatureGate(EchoApiFeature);

        return builder;
    }
}