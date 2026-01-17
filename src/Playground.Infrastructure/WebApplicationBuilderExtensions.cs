using System.Reflection;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

using Rebus.Bus;

namespace Playground.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder Initialize(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServiceProvider();
        builder.WebHost.ConfigureKestrelServer(builder.Configuration);
        builder.ConfigureLogging(builder.Configuration);

        builder.Services.AddFeatureManagement();
        builder.Services.AddVersionedOpenApi();

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.ConfigureHeaderPropagation(builder.Configuration);
        builder.Services.AddServiceDiscovery();

        builder.ConfigureOpenTelemetry(builder.Configuration);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddOneWayBus();
        builder.Services.TryAddTransient<IEventPublisher, EventPublisher>();

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
        builder.Services.UseMinimalHttpLogger();
        builder.Services.AddHttpLogging(options =>
        {
            configuration.GetSection("HttpLogging").Bind(options);
            options.CombineLogs = true;
        });
    }

    private static void ConfigureHeaderPropagation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHeaderPropagation(options =>
        {
            var headerOptions = new HttpHeaderPropagationOptions();
            configuration.GetSection("HeaderPropagation").Bind(headerOptions);

            foreach (var header in headerOptions.Headers)
            {
                options.Headers.Add(header);
            }
        });
    }

    private sealed class HttpHeaderPropagationOptions
    {
        public IList<string> Headers { get; } = [];
    }
}