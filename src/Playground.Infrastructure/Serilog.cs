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
}

public static class HostingBuilderExtensions
{
    public static IHostBuilder UseSerilogLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog(
            configureLogger: (context, services, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName),
            writeToProviders: true);
    }
}