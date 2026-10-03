using System.Text.RegularExpressions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Playground.Infrastructure.UnitTests")]

namespace Playground.Infrastructure;

internal static partial class SqlRegex
{
    public static readonly Regex SelectOneRegex = SelectOneOptimizedRegex();

    public static readonly Regex TimeoutPollingTableRegex = TimeoutPollingTableOptimizedRegex();

    public static readonly Regex InformationSchemaRegex = InformationSchemaOptimizedRegex();

    public static readonly Regex CreateIndexOnTimeoutRegex = CreateIndexOnTimeoutOptimizedRegex();

    [GeneratedRegex(@"^\s*SELECT\s+1\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, "en-US")]
    private static partial Regex SelectOneOptimizedRegex();

    [GeneratedRegex(@"\b(?:FROM|INSERT\s+INTO)\s+(?:\[?\w+\]?\.)?\[?\w*Timeout\w*\]?\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.Multiline, "en-US")]
    private static partial Regex TimeoutPollingTableOptimizedRegex();

    [GeneratedRegex(@"FROM\s+INFORMATION_SCHEMA\.TABLES\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, "en-US")]
    private static partial Regex InformationSchemaOptimizedRegex();

    [GeneratedRegex(@"CREATE\s+CLUSTERED\s+INDEX\s+[^ ]+\s+ON\s+(?:\[[^\]]+\]\.)?(?:\[[^\]]*Timeout\]|[^\s.()]*Timeout)\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, "en-US")]
    private static partial Regex CreateIndexOnTimeoutOptimizedRegex();
}