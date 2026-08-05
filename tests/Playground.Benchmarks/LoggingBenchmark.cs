using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Playground.Benchmarks;

[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(RunStrategy.Throughput)]
[MinIterationTime(250)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class LoggingBenchmark
{
    private ILogger<LoggingBenchmark>? logger = null;

    private string QuickAnimal { get; set; } = "quick brown fox";

    private string LazyAnimal { get; set; } = "lazy dog";

    [GlobalSetup]
    public void GlobalSetup()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        var app = builder.Build();

        logger = app.Services.GetRequiredService<ILogger<LoggingBenchmark>>();
    }

    [Benchmark]
    [BenchmarkCategory("Structured", "Not Logged")]
    public void StructuredParametersNotLogged()
    {
        logger?.LogDebug("The {QuickAnimal} jumps over the {LazyAnimal}", QuickAnimal, LazyAnimal);
    }

    [Benchmark]
    [BenchmarkCategory("Structured", "Logged")]
    public void StructuredParametersLogged()
    {
        logger?.LogInformation("The {QuickAnimal} jumps over the {LazyAnimal}", QuickAnimal, LazyAnimal);
    }

    [Benchmark]
    [BenchmarkCategory("Structured", "Not Logged")]
    public void StructuredLoggerMessageNotLogged()
    {
        logger?.DebugQuickLazyAnimal(QuickAnimal, LazyAnimal);
    }

    [Benchmark]
    [BenchmarkCategory("Structured", "Logged")]
    public void StructuredLoggerMessageLogged()
    {
        logger?.InformationQuickLazyAnimal(QuickAnimal, LazyAnimal);
    }

#pragma warning disable S2629 // Logging templates should be constant
    [Benchmark]
    [BenchmarkCategory("Unstructured", "Not Logged")]
    public void UnstructuredConcatenationNotLogged()
    {
        logger?.LogDebug("The " + QuickAnimal + " jumps over the " + LazyAnimal);
    }

    [Benchmark]
    [BenchmarkCategory("Unstructured", "Logged")]
    public void UnstructuredConcatenationLogged()
    {
        logger?.LogInformation("The " + QuickAnimal + " jumps over the " + LazyAnimal);
    }

    [Benchmark]
    [BenchmarkCategory("Unstructured", "Not Logged")]
    public void UnstructuredInterpolationNotLogged()
    {
        logger?.LogDebug($"The {QuickAnimal} jumps over the {LazyAnimal}");
    }

    [Benchmark]
    [BenchmarkCategory("Unstructured", "Logged")]
    public void UnstructuredInterpolationLogged()
    {
        logger?.LogInformation($"The {QuickAnimal} jumps over the {LazyAnimal}");
    }
#pragma warning restore S2629 // Logging templates should be constant
}

#pragma warning disable MA0048 // File name must match type name
internal static partial class LoggingBenchmarkExtensions
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The {QuickAnimal} jumps over the {LazyAnimal}")]
    public static partial void DebugQuickLazyAnimal(this ILogger logger, string quickAnimal, string lazyAnimal);

    [LoggerMessage(Level = LogLevel.Information, Message = "The {QuickAnimal} jumps over the {LazyAnimal}")]
    public static partial void InformationQuickLazyAnimal(this ILogger logger, string quickAnimal, string lazyAnimal);
}
#pragma warning restore MA0048 // File name must match type name