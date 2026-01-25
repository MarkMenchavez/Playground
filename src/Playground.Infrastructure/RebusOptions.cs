namespace Playground.Infrastructure;

public class RebusOptions
{
    public const string SectionName = "Rebus";

    public string InputQueueName { get; init; } = string.Empty;

    public int NumberOfWorkers { get; init; } = 1;

    public int MaxParallelism { get; init; } = 1;

    public int MaxDeliveryAttempts { get; init; } = 1;

    public bool SecondLevelRetriesEnabled { get; init; } = true;

    public int MaxDeferAttempts { get; init; } = 3;

    public int DeferDelaySeconds { get; init; } = 5;

    public int MinDeferDelaySeconds { get; init; }

    public int MaxDeferDelaySeconds { get; init; } = 3600;

    public bool UseFixedDelay { get; init; }

    public double RetryBackoffFactor { get; init; } = 1.0;

    public bool IgnoreDeferCountForBackoff { get; init; }

    public bool UseDefaultJitter { get; init; } = true;

    public double JitterMin { get; init; } = 0.9;

    public double JitterMax { get; init; } = 1.1;

    public TypeBasedMap[] TypeBasedMaps { get; init; } = [];

    public IList<string> Tenants { get; init; } = [];
}

public class TypeBasedMap
{
    public string TypeName { get; init; } = string.Empty;

    public string DestinationAddress { get; init; } = string.Empty;
}