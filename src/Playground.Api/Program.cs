using Playground.Api;
using Playground.Infrastructure;

using Serilog;

Log.Logger = SerilogExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication
        .CreateBuilder(args)
        .Initialize();

    builder.Services
        .AddWeatherForecast(builder.Configuration);

    var app = builder.Build()
        .InitializePipeline();

    app.MapWeatherForecast();

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