using System.Text;

namespace Playground.Infrastructure;

public static class StringExtensions
{
    public static string Sanitize(this string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Sanitize user input before logging to prevent log forging via control characters
        // and embedded newlines. Remove CR/LF entirely and replace other control characters
        // (except tab) with a space so the logged message remains on a single line.
        var builder = new StringBuilder(message.Length);
        foreach (var c in message)
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