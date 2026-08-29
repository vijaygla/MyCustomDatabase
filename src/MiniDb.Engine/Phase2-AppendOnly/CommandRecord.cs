namespace MiniDb.Engine.Phase2AppendOnly;

public enum OperationType
{
    Set,
    Delete
}

public class CommandRecord
{
    public OperationType Type { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // File me save karne ke liye line format: SET,key,value ya DELETE,key
    public string ToLogLine()
    {
        return Type == OperationType.Set
            ? $"SET,{Key},{Value}"
            : $"DELETE,{Key}";
    }

    public static CommandRecord? FromLogLine(string line)
    {
        var parts = line.Split(',', 3);
        if (parts.Length < 2) return null;

        if (parts[0] == "SET" && parts.Length == 3)
        {
            return new CommandRecord { Type = OperationType.Set, Key = parts[1], Value = parts[2] };
        }
        if (parts[0] == "DELETE")
        {
            return new CommandRecord { Type = OperationType.Delete, Key = parts[1] };
        }

        return null;
    }
}
