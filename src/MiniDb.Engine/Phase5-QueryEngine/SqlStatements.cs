namespace MiniDb.Engine.Phase5QueryEngine;

public abstract class SqlStatement { }

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public class CreateTableStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnDefinition> Columns { get; set; } = new();
}

public class InsertStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class SelectStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
    public bool SelectAll { get; set; } = true;
    public string? WhereColumn { get; set; }
    public string? WhereValue { get; set; }
}

public class DeleteStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
    public string? WhereColumn { get; set; }
    public string? WhereValue { get; set; }
}

public class DropTableStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
}

public class UpdateStatement : SqlStatement
{
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string? WhereColumn { get; set; }
    public string? WhereValue { get; set; }
}
