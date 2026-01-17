#pragma warning disable MA0048 // File name must match type name

using System.Text;

namespace Playground.Infrastructure;

public static class StringExtensions
{
    public static string Sanitize(this string @string)
    {
        // Sanitize user input before logging to prevent log forging via control characters
        // and embedded newlines. Remove CR/LF entirely and replace other control characters
        // (except tab) with a space so the logged message remains on a single line.
        var builder = new StringBuilder(@string.Length);
        foreach (var c in @string)
        {
            if (c == '\r' || c == '\n')
            {
                continue;
            }

            if (char.IsControl(c) && c != '\t')
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}