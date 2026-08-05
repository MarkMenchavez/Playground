using Playground.Infrastructure;

namespace Playground.Infrastructure.UnitTests;

public class StringExtensionsUnitTest
{
    [Fact(DisplayName = "Should sanitize input by removing CR/LF and replacing other control characters with spaces")]
    public void ShouldSanitizeInput()
    {
        const string input = "Hello\r\nWorld!\tThis is a test.";
        const string expectedOutput = "HelloWorld!\tThis is a test.  ";

        string actualOutput = input.Sanitize();

        Assert.Equal(expectedOutput, actualOutput);
    }

    [Fact(DisplayName = "Should throw ArgumentNullException when input is null")]
    public void ShouldThrowArgumentNullExceptionWhenInputIsNull()
    {
        const string? input = null!;

#pragma warning disable TaskAsyncInsteadAnalyzer // Replace with async method
        Assert.Throws<ArgumentNullException>(() => input.Sanitize());
#pragma warning restore TaskAsyncInsteadAnalyzer // Replace with async method
    }

    [Fact(DisplayName = "Should return empty string for empty input")]
    public void ShouldReturnEmptyStringForEmptyInput()
    {
        const string input = "";
        string actualOutput = input.Sanitize();

        Assert.Equal(input, actualOutput);
    }

    [Fact(DisplayName = "Should replace all control characters with spaces")]
    public void ShouldReplaceAllControlCharactersWithSpaces()
    {
        // Input consists of only control characters and tabs. CR/LF should be removed, others converted to spaces.
        const string input = "\r\n\t";

        // Expected behavior: \r\n are removed; the rest ( and ) are replaced with a space ' '
        // Expected behavior: \r\n are removed; the rest ( and ) are replaced with a space ' ' but tab is preserved
        const string expectedOutput = " \t ";

        string actualOutput = input.Sanitize();
        Assert.Equal(expectedOutput, actualOutput);
    }
}