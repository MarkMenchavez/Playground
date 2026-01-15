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
        app.UseHeaderPropagation();

        app.UseExceptionHandler();

        ////app.UseHsts();
        ////app.UseHttpsRedirection();

        app.MapEchoApi();
        return app;
    }
}