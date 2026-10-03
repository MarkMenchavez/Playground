using System.Text.RegularExpressions;

using Playground.Infrastructure;
using Xunit;

namespace Playground.Infrastructure.UnitTests;

public class SqlRegexTests
{
    [Theory]
    [InlineData("SELECT 1", true)]
    [InlineData("  SELECT 1", true)]
    [InlineData("select 1", true)]
    [InlineData("SELECT 2", false)]
    [InlineData("  1", false)]
    public void ShouldMatchSelectOneRegexCorrectly(string input, bool expected)
    {
        bool actual = SqlRegex.SelectOneRegex.IsMatch(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("FROM [TimeoutTable]", true)]
    [InlineData("INSERT INTO TimeoutLog", true)]
    [InlineData("FROM TimeoutSchema.TimeoutTable", true)]
    [InlineData("FROM Timeout", true)]
    [InlineData("SELECT TimeoutTable", false)]
    [InlineData("FROM RegularTable", false)]
    [InlineData("INSERT INTO RegularTable", false)]
    public void ShouldMatchTimeoutPollingTableRegexCorrectly(string input, bool expected)
    {
        bool actual = SqlRegex.TimeoutPollingTableRegex.IsMatch(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("FROM INFORMATION_SCHEMA.TABLES", true)]
    [InlineData("from information_schema.tables", true)]
    [InlineData("FROM INFORMATION_SCHEMA.VIEWS", false)]
    public void ShouldMatchInformationSchemaRegexCorrectly(string input, bool expected)
    {
        bool actual = SqlRegex.InformationSchemaRegex.IsMatch(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("CREATE CLUSTERED INDEX IX_Something ON MyTableTimeout", true)]
    [InlineData("CREATE NONCLUSTERED INDEX IX_Timeout ON MyTableTimeout", false)]
    [InlineData("ALTER INDEX MyTableTimeout", false)]
    [InlineData("CREATE INDEX RegularIndex ON MyTableTimeout", false)]
    public void ShouldMatchCreateIndexOnTimeoutRegexCorrectly(string input, bool expected)
    {
        bool actual = SqlRegex.CreateIndexOnTimeoutRegex.IsMatch(input);
        Assert.Equal(expected, actual);
    }
}