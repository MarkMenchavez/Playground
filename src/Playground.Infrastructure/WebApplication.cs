using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Playground.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder Initialize(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServiceProvider();
        builder.WebHost.ConfigureKestrelServer(builder.Configuration);
        builder.ConfigureLogging(builder.Configuration);

        builder.Services.AddVersionedOpenApi();

        return builder;
    }

    private static void ConfigureServiceProvider(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
    }

    private static void ConfigureKestrelServer(this IWebHostBuilder webHostBuilder, IConfiguration configuration)
    {
        webHostBuilder.ConfigureKestrel(options =>
        {
            configuration.GetSection("Kestrel").Bind(options);
            options.AddServerHeader = false;
        });
    }

    private static void ConfigureLogging(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseSerilogLogging();
        builder.Services.AddHttpLogging(options =>
        {
            configuration.GetSection("HttpLogging").Bind(options);
            options.CombineLogs = true;
        });
    }
}

public static class WebApplicationExtensions
{
    public static WebApplication InitializePipeline(this WebApplication app)
    {
        app.MapScalarOpenApi();

        app.UseSerilogRequestLogging();
        app.UseHttpLogging();

        ////app.UseHsts();
        ////app.UseHttpsRedirection();

        return app;
    }
}