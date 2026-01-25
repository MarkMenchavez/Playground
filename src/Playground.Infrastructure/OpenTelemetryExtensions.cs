using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Rebus.OpenTelemetry.Configuration;

namespace Playground.Infrastructure;

internal static partial class OpenTelemetryExtensions
{
    private static readonly Regex SelectOneRegex = SelectOneOptimizedRegex();

    private static readonly Regex TimeoutPollingTableRegex = TimeoutPollingTableOptimizedRegex();

    private static readonly Regex InformationSchemaRegex = InformationSchemaOptimizedRegex();

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

        telemetryBuilder.WithTracing(ConfigureTracerBuilder);
        telemetryBuilder.WithMetrics(ConfigureMetricsBuilder);

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

    private static void ConfigureTracerBuilder(TracerProviderBuilder tracing)
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

        tracing.AddSqlClientInstrumentation(options =>
        {
            options.Filter = context => ShouldTraceSqlCommand(context);
            options.RecordException = true;
        });

        tracing.AddRebusInstrumentation();
    }

    private static bool ShouldTraceSqlCommand(object context)
    {
        if (context is not SqlCommand command)
        {
            return false;
        }

        var sql = command.CommandText;

        return !string.IsNullOrWhiteSpace(sql) &&
            !SelectOneRegex.IsMatch(sql) &&
            !TimeoutPollingTableRegex.IsMatch(sql) &&
            !InformationSchemaRegex.IsMatch(sql);
    }

    private static void ConfigureMetricsBuilder(MeterProviderBuilder metrics)
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddSqlClientInstrumentation();
        metrics.AddRebusInstrumentation();
    }

    [GeneratedRegex(@"^\s*SELECT\s+1\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SelectOneOptimizedRegex();

    [GeneratedRegex(@"\b(?:FROM|INSERT\s+INTO)\s+(?:\[?\w+\]?\.)?\[?\w*Timeout\w*\]?\b", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    private static partial Regex TimeoutPollingTableOptimizedRegex();

    [GeneratedRegex(@"FROM\s+INFORMATION_SCHEMA\.TABLES\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex InformationSchemaOptimizedRegex();
}