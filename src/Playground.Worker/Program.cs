using Playground.Infrastructure;
using Playground.Worker;

using Serilog;

Log.Logger = SerilogExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication
        .CreateBuilder(args)
        .Initialize();

    builder.Host.AddWeatherForecastHandler();

    var app = builder.Build()
        .InitializePipeline();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}