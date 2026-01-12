using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Hosting;

namespace Playground.Infrastructure;

public static class SerilogExtensions
{
    public static ReloadableLogger CreateBootstrapLogger()
    {
        return new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
}

internal static class HostBuilderExtensions
{
    public static IHostBuilder UseSerilogLogging(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.AddHttpContextAccessor();
        });

        return hostBuilder.UseSerilog(
            configureLogger: (context, services, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.With<ShortSourceContextEnricher>()
                    .Enrich.WithExceptionDetails()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .Enrich.WithCorrelationId(addValueIfHeaderAbsence: true),
            writeToProviders: true);
    }
}

internal sealed class ShortSourceContextEnricher : ILogEventEnricher
{
    public const string ShortPropertyName = "ShortSourceContext";
    private const string SourceContextPropertyName = "SourceContext";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue(SourceContextPropertyName, out var value))
        {
            return;
        }

        if (value is not ScalarValue scalar || scalar.Value is not string sourceContext)
        {
            return;
        }

        // Strategy: last segment only (e.g. UsersController)
        var segments = sourceContext.Split('.');
        var shortContext = segments[^1];

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(ShortPropertyName, shortContext));
    }
}