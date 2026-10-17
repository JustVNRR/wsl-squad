using System.Text;

namespace wsl_squad.Core.Infrastructure.Json;

/// <summary>Removes comments and trailing commas from JSONC text.</summary>
public static class JsoncSanitizer
{
    public static string Sanitize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return RemoveTrailingCommas(RemoveComments(json));
    }

    private static string RemoveComments(string json)
    {
        StringBuilder builder = new(json.Length);
        bool inString = false;
        bool escaped = false;
        int index = 0;

        while (index < json.Length)
        {
            char current = json[index];

            if (inString)
            {
                _ = builder.Append(current);

                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                index++;
                continue;
            }

            if (current == '"')
            {
                inString = true;
                _ = builder.Append(current);
                index++;
                continue;
            }

            if (current == '/' && index + 1 < json.Length && json[index + 1] == '/')
            {
                index += 2;

                while (index < json.Length && json[index] != '\n')
                {
                    index++;
                }

                if (index < json.Length)
                {
                    _ = builder.Append('\n');
                    index++;
                }

                continue;
            }

            if (current == '/' && index + 1 < json.Length && json[index + 1] == '*')
            {
                index += 2;

                while (index + 1 < json.Length && !(json[index] == '*' && json[index + 1] == '/'))
                {
                    index++;
                }

                index = Math.Min(index + 2, json.Length);
                continue;
            }

            _ = builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static string RemoveTrailingCommas(string json)
    {
        StringBuilder builder = new(json);
        int index = 0;

        while (index < builder.Length)
        {
            if (builder[index] is '}' or ']')
            {
                int previous = index - 1;

                while (previous >= 0 && char.IsWhiteSpace(builder[previous]))
                {
                    previous--;
                }

                if (previous >= 0 && builder[previous] == ',')
                {
                    _ = builder.Remove(previous, 1);
                    index = previous;
                    continue;
                }
            }

            index++;
        }

        return builder.ToString();
    }
}
