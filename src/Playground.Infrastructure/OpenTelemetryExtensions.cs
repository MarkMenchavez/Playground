using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Rebus.OpenTelemetry.Configuration;

namespace Playground.Infrastructure;

internal static class OpenTelemetryExtensions
{
    public static void ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        var telemetryBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceNamespace: null,
                    serviceVersion: null,
                    autoGenerateServiceInstanceId: false,
                    serviceInstanceId: Environment.MachineName)
                .AddEnvironmentVariableDetector());

        telemetryBuilder.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.EnableAspNetCoreSignalRSupport = true;
                options.RecordException = true;
            });

            tracing.AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            });

            tracing.AddRebusInstrumentation();
        });

        telemetryBuilder.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddRebusInstrumentation();
        });

        var enableOtelLogging = builder.Configuration.GetValue<bool>("FeatureManagement:EnableOtelLogging");
        if (enableOtelLogging)
        {
            telemetryBuilder.WithLogging(_ => { }, options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
            });
        }

        telemetryBuilder.UseOtlpExporter();
    }
}