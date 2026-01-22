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

    public int DeferDelaySeconds { get; init; } = 15;

    public TypeBasedMap[] TypeBasedMaps { get; init; } = [];

    public IList<string> Tenants { get; init; } = [];
}

public class TypeBasedMap
{
    public string TypeName { get; init; } = string.Empty;

    public string DestinationAddress { get; init; } = string.Empty;
}