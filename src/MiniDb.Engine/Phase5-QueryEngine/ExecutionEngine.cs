using MiniDb.Engine.Phase4Indexing;

namespace MiniDb.Engine.Phase5QueryEngine;

public class ExecutionEngine
{
    private readonly BPlusTree _bTree;
    private readonly Dictionary<string, CreateTableStatement> _tables = new(StringComparer.OrdinalIgnoreCase);

    public ExecutionEngine(BPlusTree bTree)
    {
        _bTree = bTree;
    }

    public string Execute(string sqlQuery)
    {
        var lexer = new SqlLexer(sqlQuery);
        var tokens = lexer.Tokenize();
        var parser = new SqlParser(tokens);
        var statement = parser.Parse();

        return statement switch
        {
            CreateTableStatement create => ExecuteCreateTable(create),
            InsertStatement insert => ExecuteInsert(insert),
            SelectStatement select => ExecuteSelect(select),
            DeleteStatement delete => ExecuteDelete(delete),
            DropTableStatement drop => ExecuteDropTable(drop),
            _ => "Error: Unsupported SQL statement"
        };
    }

    private string ExecuteCreateTable(CreateTableStatement stmt)
    {
        if (_tables.ContainsKey(stmt.TableName))
        {
            return $"Error: Table '{stmt.TableName}' already exists.";
        }

        _tables[stmt.TableName] = stmt;
        var colSummary = string.Join(", ", stmt.Columns.Select(c => $"{c.Name} {c.DataType}"));
        return $"Table '{stmt.TableName}' created successfully with schema ({colSummary}).";
    }

    private string ExecuteInsert(InsertStatement stmt)
    {
        if (!_tables.ContainsKey(stmt.TableName))
        {
            _tables[stmt.TableName] = new CreateTableStatement { TableName = stmt.TableName };
        }

        if (stmt.Values.Count < 2)
        {
            return "Error: INSERT requires at least 2 values (Primary Key and Row Data).";
        }

        string primaryKey = stmt.Values[0];
        string rowValue = string.Join("|", stmt.Values.Skip(1));
        string compositeKey = $"{stmt.TableName}:{primaryKey}";

        _bTree.Insert(compositeKey, rowValue);
        return $"1 row inserted into '{stmt.TableName}'. Persistent B+ Tree Key: '{compositeKey}'";
    }

    private string ExecuteSelect(SelectStatement stmt)
    {
        if (stmt.WhereColumn != null && stmt.WhereValue != null)
        {
            string compositeKey = $"{stmt.TableName}:{stmt.WhereValue}";
            var value = _bTree.Search(compositeKey);

            if (value == null)
            {
                return "(0 rows returned)";
            }

            return $"[1 Row Found]\n{stmt.WhereColumn} = {stmt.WhereValue} | Data = {value}";
        }

        var allKeys = _bTree.GetAllKeys();
        var tablePrefix = $"{stmt.TableName}:";
        var matchingRows = allKeys.Where(k => k.Key.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matchingRows.Count == 0)
        {
            return "(0 rows returned)";
        }

        var result = $"--- Table: {stmt.TableName} ({matchingRows.Count} rows) ---\n";
        foreach (var kvp in matchingRows)
        {
            string id = kvp.Key.Substring(tablePrefix.Length);
            result += $"ID: {id} | Values: {kvp.Value}\n";
        }

        return result.TrimEnd();
    }

    private string ExecuteDelete(DeleteStatement stmt)
    {
        if (stmt.WhereColumn != null && stmt.WhereValue != null)
        {
            string compositeKey = $"{stmt.TableName}:{stmt.WhereValue}";
            bool deleted = _bTree.Delete(compositeKey);
            return deleted ? $"Row with {stmt.WhereColumn} = '{stmt.WhereValue}' deleted from table '{stmt.TableName}'." : "(0 rows affected)";
        }

        var allKeys = _bTree.GetAllKeys();
        var tablePrefix = $"{stmt.TableName}:";
        int count = 0;

        foreach (var kvp in allKeys)
        {
            if (kvp.Key.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                _bTree.Delete(kvp.Key);
                count++;
            }
        }

        return $"{count} row(s) deleted from table '{stmt.TableName}'.";
    }

    private string ExecuteDropTable(DropTableStatement stmt)
    {
        var allKeys = _bTree.GetAllKeys();
        var tablePrefix = $"{stmt.TableName}:";
        int count = 0;

        foreach (var kvp in allKeys)
        {
            if (kvp.Key.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                _bTree.Delete(kvp.Key);
                count++;
            }
        }

        _tables.Remove(stmt.TableName);
        return $"Table '{stmt.TableName}' dropped successfully. ({count} stored rows removed)";
    }
}
