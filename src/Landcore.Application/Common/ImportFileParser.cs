using System.Text;
using Landcore.Application.Exceptions;

namespace Landcore.Application.Common;

public static class ImportFileParser
{
    public static List<Dictionary<string, string>> ParseRows(string fileContent)
    {
        var lines = fileContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            throw new ValidationAppException(
                "The uploaded file is empty.",
                new Dictionary<string, string[]> { ["File"] = ["The uploaded file is empty."] });
        }

        var headerLine = lines[0];
        var delimiter = headerLine.Contains('\t')
            ? '\t'
            : headerLine.Contains(';') && !headerLine.Contains(',')
                ? ';'
                : ',';

        var headers = SplitLine(headerLine, delimiter).Select(header => header.Trim()).ToList();

        var rows = new List<Dictionary<string, string>>(lines.Count - 1);
        for (var i = 1; i < lines.Count; i++)
        {
            var fields = SplitLine(lines[i], delimiter);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
            {
                row[headers[col]] = col < fields.Count ? fields[col].Trim() : string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
