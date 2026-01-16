using Microsoft.AspNetCore.Builder;

using Serilog;

namespace Playground.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication InitializePipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.MapScalarOpenApi();

        app.UseSerilogRequestLogging();
        app.UseHttpLogging();
        app.UseHeaderPropagation();

        app.UseExceptionHandler();

        app.MapEchoApi();
        return app;
    }
}