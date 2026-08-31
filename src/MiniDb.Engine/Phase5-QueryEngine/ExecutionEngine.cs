using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase6WAL;

namespace MiniDb.Engine.Phase5QueryEngine;

public enum TransactionActionType { Insert, Delete, Update }

public class TransactionBufferItem
{
    public TransactionActionType Action { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}

public class ExecutionEngine
{
    private readonly BPlusTree _bTree;
    private readonly WalManager? _walManager;
    private readonly Dictionary<string, CreateTableStatement> _tables = new(StringComparer.OrdinalIgnoreCase);

    private bool _inTransaction = false;
    private readonly List<TransactionBufferItem> _txBuffer = new();

    public ExecutionEngine(BPlusTree bTree, WalManager? walManager = null)
    {
        _bTree = bTree;
        _walManager = walManager;
        PerformCrashRecovery();
    }

    public List<string> GetTableNames()
    {
        return _tables.Keys.ToList();
    }

    private void PerformCrashRecovery()
    {
        if (_walManager == null) return;

        var logs = _walManager.RecoverLogRecords();
        foreach (var log in logs)
        {
            if (log.Type == LogRecordType.Insert)
            {
                try { _bTree.Insert(log.Key, log.Value); } catch { }
            }
            else if (log.Type == LogRecordType.Delete)
            {
                _bTree.Delete(log.Key);
            }
            else if (log.Type == LogRecordType.Update)
            {
                string[] parts = log.Value.Split('|');
                string newValue = parts.Length > 1 ? parts[1] : parts[0];
                _bTree.Update(log.Key, newValue);
            }
        }
    }

    public string Execute(string sqlQuery)
    {
        var lexer = new SqlLexer(sqlQuery);
        var tokens = lexer.Tokenize();
        var parser = new SqlParser(tokens);
        var statement = parser.Parse();

        return statement switch
        {
            BeginTransactionStatement => ExecuteBegin(),
            CommitTransactionStatement => ExecuteCommit(),
            RollbackTransactionStatement => ExecuteRollback(),
            CreateTableStatement create => ExecuteCreateTable(create),
            InsertStatement insert => ExecuteInsert(insert),
            SelectStatement select => ExecuteSelect(select),
            DeleteStatement delete => ExecuteDelete(delete),
            UpdateStatement update => ExecuteUpdate(update),
            DropTableStatement drop => ExecuteDropTable(drop),
            _ => "Error: Unsupported SQL statement"
        };
    }

    private string ExecuteBegin()
    {
        if (_inTransaction) return "Error: Transaction is already active.";
        _inTransaction = true;
        _txBuffer.Clear();
        return "Transaction started. (BEGIN)";
    }

    private string ExecuteCommit()
    {
        if (!_inTransaction) return "Error: No active transaction to commit.";

        int count = 0;
        foreach (var item in _txBuffer)
        {
            if (item.Action == TransactionActionType.Insert)
            {
                if (_bTree.Search(item.Key) != null) _bTree.Delete(item.Key);
                _walManager?.WriteRecord(LogRecordType.Insert, item.Key, item.Value);
                try { _bTree.Insert(item.Key, item.Value); } catch { }
            }
            else if (item.Action == TransactionActionType.Delete)
            {
                _walManager?.WriteRecord(LogRecordType.Delete, item.Key, "");
                _bTree.Delete(item.Key);
            }
            else if (item.Action == TransactionActionType.Update)
            {
                _walManager?.WriteRecord(LogRecordType.Update, item.Key, item.Value);
                string[] parts = item.Value.Split('|');
                string newValue = parts.Length > 1 ? parts[1] : parts[0];
                _bTree.Update(item.Key, newValue);
            }
            count++;
        }

        _txBuffer.Clear();
        _inTransaction = false;
        return $"Transaction committed successfully. ({count} operation(s) applied)";
    }

    private string ExecuteRollback()
    {
        if (!_inTransaction) return "Error: No active transaction to rollback.";
        int discarded = _txBuffer.Count;
        _txBuffer.Clear();
        _inTransaction = false;
        return $"Transaction rolled back. ({discarded} operation(s) discarded)";
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

        string primaryKey = stmt.Values[0].Trim('\'', '"');
        string rowValue = string.Join(", ", stmt.Values.Skip(1).Select(v => v.Trim('\'', '"')));
        string compositeKey = $"{stmt.TableName}:{primaryKey}";

        if (_inTransaction)
        {
            _txBuffer.Add(new TransactionBufferItem
            {
                Action = TransactionActionType.Insert,
                TableName = stmt.TableName,
                Key = compositeKey,
                Value = rowValue
            });
            return $"[TX Buffered] 1 row staged for INSERT in table '{stmt.TableName}'.";
        }

        if (_bTree.Search(compositeKey) != null)
        {
            _bTree.Delete(compositeKey);
        }

        _walManager?.WriteRecord(LogRecordType.Insert, compositeKey, rowValue);
        _bTree.Insert(compositeKey, rowValue);

        return $"1 row inserted into '{stmt.TableName}'. Persistent B+ Tree Key: '{compositeKey}'";
    }

    private string ExecuteUpdate(UpdateStatement stmt)
    {
        if (stmt.WhereColumn == null || stmt.WhereValue == null)
        {
            return "Error: UPDATE requires a valid WHERE clause.";
        }

        string cleanWhereVal = stmt.WhereValue.Trim('\'', '"');
        string cleanNewVal = stmt.NewValue.Trim('\'', '"');
        string compositeKey = $"{stmt.TableName}:{cleanWhereVal}";
        string? existingValue = null;

        if (_inTransaction)
        {
            var buffered = _txBuffer.LastOrDefault(b => b.Key.Equals(compositeKey, StringComparison.OrdinalIgnoreCase));
            if (buffered != null)
            {
                if (buffered.Action == TransactionActionType.Delete) return "(0 rows affected - row deleted in transaction)";
                existingValue = buffered.Action == TransactionActionType.Update ? buffered.Value.Split('|').Last() : buffered.Value;
            }
        }

        if (existingValue == null)
        {
            existingValue = _bTree.Search(compositeKey);
        }

        if (existingValue == null)
        {
            return $"(0 rows affected) Key '{cleanWhereVal}' not found in table '{stmt.TableName}'.";
        }

        string finalRowValue = cleanNewVal;
        if (_tables.TryGetValue(stmt.TableName, out var tableSchema) && tableSchema.Columns.Count > 0)
        {
            var parts = existingValue.Split(',').Select(p => p.Trim('\'', ' ')).ToList();

            int colIndex = -1;
            for (int i = 0; i < tableSchema.Columns.Count; i++)
            {
                if (tableSchema.Columns[i].Name.Equals(stmt.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex = i;
                    break;
                }
            }

            if (colIndex > 0 && (colIndex - 1) < parts.Count)
            {
                parts[colIndex - 1] = cleanNewVal;
                finalRowValue = string.Join(", ", parts);
            }
        }

        if (_inTransaction)
        {
            var existingTxItem = _txBuffer.FirstOrDefault(b => b.Key.Equals(compositeKey, StringComparison.OrdinalIgnoreCase));
            if (existingTxItem != null && existingTxItem.Action == TransactionActionType.Insert)
            {
                existingTxItem.Value = finalRowValue;
            }
            else
            {
                _txBuffer.Add(new TransactionBufferItem
                {
                    Action = TransactionActionType.Update,
                    TableName = stmt.TableName,
                    Key = compositeKey,
                    Value = finalRowValue
                });
            }
            return $"[TX Buffered] Row with {stmt.WhereColumn} = '{cleanWhereVal}' staged for UPDATE.";
        }

        _walManager?.WriteRecord(LogRecordType.Update, compositeKey, finalRowValue);
        bool updated = _bTree.Update(compositeKey, finalRowValue);
        return updated ? $"1 row updated in table '{stmt.TableName}'." : "(0 rows affected)";
    }

    private string ExecuteSelect(SelectStatement stmt)
    {
        if (stmt.WhereColumn != null && stmt.WhereValue != null)
        {
            string cleanWhereVal = stmt.WhereValue.Trim('\'', '"');
            string compositeKey = $"{stmt.TableName}:{cleanWhereVal}";

            if (_inTransaction)
            {
                var buffered = _txBuffer.LastOrDefault(b => b.Key.Equals(compositeKey, StringComparison.OrdinalIgnoreCase));
                if (buffered != null)
                {
                    if (buffered.Action == TransactionActionType.Delete) return "(0 rows returned)";
                    string valToShow = buffered.Action == TransactionActionType.Update ? buffered.Value.Split('|').Last() : buffered.Value;
                    return $"[1 Row Found]\nid = {cleanWhereVal} | Data = {valToShow}";
                }
            }

            var value = _bTree.Search(compositeKey);
            if (value == null) return "(0 rows returned)";

            return $"[1 Row Found]\nid = {cleanWhereVal} | Data = {value}";
        }

        var allKeys = _bTree.GetAllKeys();
        var tablePrefix = $"{stmt.TableName}:";

        var matchingRows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in allKeys.Where(k => k.Key.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            matchingRows[k.Key] = k.Value;
        }

        if (_inTransaction)
        {
            foreach (var item in _txBuffer.Where(i => i.TableName.Equals(stmt.TableName, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.Action == TransactionActionType.Insert)
                {
                    matchingRows[item.Key] = item.Value;
                }
                else if (item.Action == TransactionActionType.Delete)
                {
                    matchingRows.Remove(item.Key);
                }
                else if (item.Action == TransactionActionType.Update)
                {
                    if (matchingRows.ContainsKey(item.Key))
                    {
                        matchingRows[item.Key] = item.Value.Split('|').Last();
                    }
                }
            }
        }

        if (matchingRows.Count == 0) return "(0 rows returned)";

        var result = $"--- Table: {stmt.TableName} ({matchingRows.Count} rows) ---\n";
        foreach (var kvp in matchingRows.OrderBy(k => k.Key))
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
            string cleanWhereVal = stmt.WhereValue.Trim('\'', '"');
            string compositeKey = $"{stmt.TableName}:{cleanWhereVal}";

            if (_inTransaction)
            {
                _txBuffer.Add(new TransactionBufferItem
                {
                    Action = TransactionActionType.Delete,
                    TableName = stmt.TableName,
                    Key = compositeKey,
                    Value = ""
                });
                return $"[TX Buffered] Row with {stmt.WhereColumn} = '{cleanWhereVal}' staged for DELETE.";
            }

            _walManager?.WriteRecord(LogRecordType.Delete, compositeKey, "");
            bool deleted = _bTree.Delete(compositeKey);
            return deleted ? $"Row with {stmt.WhereColumn} = '{cleanWhereVal}' deleted from table '{stmt.TableName}'." : "(0 rows affected)";
        }

        var allKeys = _bTree.GetAllKeys();
        var tablePrefix = $"{stmt.TableName}:";
        int count = 0;

        foreach (var kvp in allKeys)
        {
            if (kvp.Key.StartsWith(tablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (_inTransaction)
                {
                    _txBuffer.Add(new TransactionBufferItem
                    {
                        Action = TransactionActionType.Delete,
                        TableName = stmt.TableName,
                        Key = kvp.Key,
                        Value = ""
                    });
                }
                else
                {
                    _walManager?.WriteRecord(LogRecordType.Delete, kvp.Key, "");
                    _bTree.Delete(kvp.Key);
                }
                count++;
            }
        }

        return _inTransaction
            ? $"[TX Buffered] {count} row(s) staged for DELETE in table '{stmt.TableName}'."
            : $"{count} row(s) deleted from table '{stmt.TableName}'.";
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
                _walManager?.WriteRecord(LogRecordType.Delete, kvp.Key, "");
                _bTree.Delete(kvp.Key);
                count++;
            }
        }

        _tables.Remove(stmt.TableName);
        return $"Table '{stmt.TableName}' dropped successfully. ({count} stored rows removed)";
    }
}
