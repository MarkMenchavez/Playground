using Microsoft.AspNetCore.Builder;
using Playground.Api;
using Playground.Infrastructure;
using Serilog;

Log.Logger = SerilogExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseSerilogLogging();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddWeatherForecast(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    ////app.UseHsts();
    ////app.UseHttpsRedirection();

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