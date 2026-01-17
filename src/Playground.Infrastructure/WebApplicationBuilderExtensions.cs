using System.Reflection;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Playground.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder Initialize(this WebApplicationBuilder builder)
    {
        builder.Host.ConfigureServiceProvider();
        builder.WebHost.ConfigureKestrelServer(builder.Configuration);

        builder.ConfigureLogging();
        builder.ConfigureOpenTelemetry();

        builder.Services.AddFeatureManagement();

        builder.Services.AddVersionedOpenApi();
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.ConfigureHeaderPropagation(builder.Configuration);
        builder.Services.AddServiceDiscovery();

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
            // IMPORTANT:
            // ValidateOnBuild MUST NOT be enabled in Production.
            //
            // In Kubernetes, enabling ValidateOnBuild in Production causes pods to
            // crash at startup for *any* invalid or unused DI registration, which:
            //   - Blocks rollouts
            //   - Causes CrashLoopBackOff
            //   - Can deadlock Deployments / StatefulSets
            //
            // ValidateOnBuild is intentionally limited to Development/Staging to
            // catch broken registrations *before* they reach Production.
            // If you think you need this in Production, stop and re-evaluate the
            // DI graph and CI validation strategy first.
            if (context.HostingEnvironment.IsDevelopment() ||
                context.HostingEnvironment.IsStaging())
            {
                options.ValidateOnBuild = true;
            }

            // ValidateScopes is ALWAYS enabled, including Production.
            // Scoped-to-singleton lifetime violations are deterministic bugs that
            // WILL cause data corruption, memory leaks, or threading issues.
            // These must fail fast, even in Production.
            options.ValidateScopes = true;
        });
    }

    private static void ConfigureKestrelServer(this IWebHostBuilder webHostBuilder, IConfiguration configuration)
    {
        webHostBuilder.ConfigureKestrel(options =>
        {
            configuration.GetSection("Kestrel").Bind(options);

            // SECURITY:
            // Do NOT expose the 'Server' response header.
            // This prevents unnecessary information disclosure and is required
            // by common security baselines (CIS, OWASP hardening guidance).
            //
            // This must remain disabled in all environments.
            options.AddServerHeader = false;

            options.ConfigureEndpointDefaults(listenOptions =>
            {
                // HTTP/2 has known compression and DoS attack vectors (HPACK abuse).
                // Only disable if you don’t need gRPC or HTTP/2 features.
                listenOptions.Protocols = HttpProtocols.Http1;
            });

            // The following values are commented out, but are loaded from appsettings to avoid hard-coding values.

            // Kestrel’s default is high. That’s a DoS risk.
            // Need protections against maliciously large payloads.
            // This must be set to a reasonable limit.
            // You can override per endpoint in ASP.NET Core if needed using [RequestSizeLimit].
            ////options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;               // default is 30 * 1024 * 1024

            // Protects against resource exhaustion
            // Don’t set too low in high-load services; tune according to pod resources.
            ////options.Limits.MaxConcurrentConnections = 100;                      // default is unlimited
            ////options.Limits.MaxConcurrentUpgradedConnections = 50;               // default is unlimited
            ////options.Limits.MaxRequestBufferSize = 32 * 1024;                    // default is 1024 * 1024; must be >= MaxRequestHeadersTotalSize

            // Prevents header injection / DoS.
            // Reverse proxies may add large headers (e.g., X-Forwarded-*), plan accordingly.
            ////options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;              // default is 32 * 1024

            // Prevents buffer overflow attempts / header attacks
            ////options.Limits.MaxRequestLineSize = 8 * 1024;                       // default is 8 * 1024
            ////options.Limits.MaxRequestHeaderCount = 100;                         // default is 100

            // Mitigates slowloris attacks
            // Don’t go too low if clients take time (mobile networks, high latency).
            ////options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(15);         // default is 00:02:10
            ////options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);    // default is 00:00:30
        });
    }

    private static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseSerilogLogging();
        builder.Services.UseMinimalHttpLogger();
        builder.Services.AddHttpLogging(options =>
        {
            builder.Configuration.GetSection("HttpLogging").Bind(options);
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