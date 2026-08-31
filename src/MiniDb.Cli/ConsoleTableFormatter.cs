using System.Text;

namespace MiniDb.Cli;

public static class ConsoleTableFormatter
{
    public static void PrintFormattedOutput(string executionResult)
    {
        if (string.IsNullOrWhiteSpace(executionResult)) return;

        // Agar result multi-row SELECT output format me hai
        if (executionResult.StartsWith("--- Table:"))
        {
            var lines = executionResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string headerInfo = lines[0]; // Header description

            List<string> headers = new() { "KEY / ID", "ROW VALUES" };
            List<List<string>> rows = new();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("| Values:"))
                {
                    var parts = line.Split(" | Values: ");
                    string id = parts[0].Replace("ID:", "").Trim();
                    string values = parts.Length > 1 ? parts[1].Trim() : "";
                    rows.Add(new List<string> { id, values });
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(headerInfo);
            Console.ResetColor();

            if (rows.Count > 0)
            {
                RenderTable(headers, rows);
            }
            return;
        }

        // Agar single row SELECT lookup result hai
        if (executionResult.StartsWith("[1 Row Found"))
        {
            var lines = executionResult.Split('\n');
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(lines[0]); // [1 Row Found]
            Console.ResetColor();

            if (lines.Length > 1)
            {
                var parts = lines[1].Split(" | Data = ");
                var headers = new List<string> { "COLUMN / WHERE", "DATA" };
                var rows = new List<List<string>>
                {
                    new List<string> { parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "" }
                };
                RenderTable(headers, rows);
            }
            return;
        }

        // Standard response/messages (Insert, Delete, Transaction status, errors)
        if (executionResult.StartsWith("Error:") || executionResult.StartsWith("[Execution Error]"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(executionResult);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(executionResult);
            Console.ResetColor();
        }
    }

    public static void RenderTable(List<string> headers, List<List<string>> rows)
    {
        int[] columnWidths = new int[headers.Count];
        for (int i = 0; i < headers.Count; i++)
        {
            columnWidths[i] = headers[i].Length;
        }

        foreach (var row in rows)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (row[i].Length > columnWidths[i])
                {
                    columnWidths[i] = row[i].Length;
                }
            }
        }

        string separator = "+" + string.Join("+", columnWidths.Select(w => new string('-', w + 2))) + "+";

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(separator);

        StringBuilder headerLine = new("|");
        for (int i = 0; i < headers.Count; i++)
        {
            headerLine.Append($" {headers[i].PadRight(columnWidths[i])} |");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(headerLine.ToString());

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(separator);
        Console.ResetColor();

        foreach (var row in rows)
        {
            StringBuilder rowLine = new("|");
            for (int i = 0; i < row.Count; i++)
            {
                rowLine.Append($" {row[i].PadRight(columnWidths[i])} |");
            }
            Console.WriteLine(rowLine.ToString());
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(separator);
        Console.ResetColor();
    }
}
