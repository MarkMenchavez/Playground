using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.AspNetCore;

namespace Playground.Infrastructure;

public interface IEchoServiceAgent
{
    Task<string> EchoAsync(string message, CancellationToken cancellationToken = default);
}

public static class EchoServiceCollectionExtensions
{
    public static IServiceCollection AddEchoServiceAgent(this IServiceCollection services)
    {
        services.AddHttpClient<IEchoServiceAgent, EchoServiceAgent>(client =>
            client.BaseAddress = new("https://echoapi"))
        .AddServiceDiscovery()
        .AddHeaderPropagation()
        .AddStandardResilienceHandler();

        return services;
    }
}

internal static class EchoConstants
{
    public const string EchoApiFeature = "EchoApi";
}

internal static class EchoEndpointBuilderExtensions
{
    public static IEndpointRouteBuilder MapEchoApi(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/echo", async (HttpContext context, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("EchoApi");

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var message = await reader.ReadToEndAsync(cancellationToken);

            logger.EchoReceived(message.Sanitize());

            return Results.Text(message, "text/plain", Encoding.UTF8);
        })
        .WithFeatureGate(EchoConstants.EchoApiFeature);

        return builder;
    }
}

internal static partial class EchoApiLogger
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Received echo message: {Message}")]
    public static partial void EchoReceived(
        this ILogger logger,
        string message);
}

internal class EchoServiceAgent(
    HttpClient httpClient,
    IFeatureManager featureManager)
    : IEchoServiceAgent
{
    public async Task<string> EchoAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!await featureManager.IsEnabledAsync(EchoConstants.EchoApiFeature, cancellationToken))
        {
            return string.Empty;
        }

        var content = new StringContent(message, Encoding.UTF8, "text/plain");

        var response = await httpClient.PostAsync("/api/echo", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}