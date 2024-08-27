using System.Globalization;
using Serilog;
using Serilog.Extensions.Hosting;

internal static class SerilogExtensions
{
    public static ReloadableLogger CreateBootstrapLogger() =>
        new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();
    
    public static void UseSerilog(this IHostBuilder hostBuilder) =>
        hostBuilder.UseSerilog(
            configureLogger: (context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName),
            writeToProviders: true);
}