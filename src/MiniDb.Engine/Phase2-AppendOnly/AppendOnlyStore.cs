using System.Collections.Concurrent;
using MiniDb.Engine.Phase1InMemory;

namespace MiniDb.Engine.Phase2AppendOnly;

public class AppendOnlyStore : IKeyValueStore
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, string> _index = new();
    private readonly object _fileLock = new();

    public AppendOnlyStore(string filePath = "data.db")
    {
        _filePath = filePath;
        LoadFromDisk(); // App start hone par purana log recover karega
    }

    public void Set(string key, string value)
    {
        var record = new CommandRecord { Type = OperationType.Set, Key = key, Value = value };
        AppendToLog(record);
        _index[key] = value;
    }

    public string? Get(string key)
    {
        return _index.TryGetValue(key, out var value) ? value : null;
    }

    public bool Delete(string key)
    {
        if (!_index.ContainsKey(key)) return false;

        var record = new CommandRecord { Type = OperationType.Delete, Key = key };
        AppendToLog(record);
        return _index.TryRemove(key, out _);
    }

    private void AppendToLog(CommandRecord record)
    {
        lock (_fileLock)
        {
            File.AppendAllLines(_filePath, new[] { record.ToLogLine() });
        }
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_filePath)) return;

        var lines = File.ReadAllLines(_filePath);
        foreach (var line in lines)
        {
            var record = CommandRecord.FromLogLine(line);
            if (record == null) continue;

            if (record.Type == OperationType.Set)
            {
                _index[record.Key] = record.Value;
            }
            else if (record.Type == OperationType.Delete)
            {
                _index.TryRemove(record.Key, out _);
            }
        }
    }
}
