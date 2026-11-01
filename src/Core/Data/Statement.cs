namespace Core.Data;

/// <summary>One row of a downloaded CSV statement.</summary>
public sealed record StatementRow(string Date, string Description, string Memo, string Type, string Amount, string Balance);

/// <summary>
/// A CSV statement as the bank sends it: a header, one row per transaction,
/// and a last "Total" row with the amounts added up.
/// </summary>
public sealed record Statement(string Header, IReadOnlyList<StatementRow> Rows, string Total)
{
    public const string ExpectedHeader = "Date (UTC),Description,Memo,Type,Amount,Balance";

    public static Statement Parse(string csv)
    {
        var lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var rows = lines[1..^1].Select(line =>
        {
            var cells = SplitCsvLine(line);
            return new StatementRow(cells[0], cells[1], cells[2], cells[3], cells[4], cells[5]);
        }).ToList();

        return new Statement(lines[0], rows, SplitCsvLine(lines[^1])[4]);
    }

    /// <summary>Splits one CSV line, keeping commas inside "quoted" cells.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var cell = new System.Text.StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { cells.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(c);
        }

        cells.Add(cell.ToString());
        return cells;
    }
}
