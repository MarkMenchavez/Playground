using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
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