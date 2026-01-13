using Serilog.Core;
using Serilog.Events;

namespace Playground.Infrastructure;

internal sealed class ShortSourceContextEnricher : ILogEventEnricher
{
    public const string ShortPropertyName = "ShortSourceContext";
    private const string SourceContextPropertyName = "SourceContext";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue(SourceContextPropertyName, out var value))
        {
            return;
        }

        if (value is not ScalarValue scalar || scalar.Value is not string sourceContext)
        {
            return;
        }

        // Strategy: last segment only (e.g. UsersController)
        var segments = sourceContext.Split('.');
        var shortContext = segments[^1];

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(ShortPropertyName, shortContext));
    }
}