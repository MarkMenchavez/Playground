using Microsoft.AspNetCore.Builder;

using Serilog;

namespace Playground.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication InitializePipeline(this WebApplication app)
    {
        app.MapScalarOpenApi();

        app.UseSerilogRequestLogging();
        app.UseHttpLogging();

        app.UseExceptionHandler();

        ////app.UseHsts();
        ////app.UseHttpsRedirection();

        return app;
    }
}