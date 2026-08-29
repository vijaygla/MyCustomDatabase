using System.Collections.Concurrent;

namespace MiniDb.Engine.Phase1InMemory;

public class InMemoryStore : IKeyValueStore
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public void Set(string key, string value)
    {
        _store[key] = value;
    }

    public string? Get(string key)
    {
        return _store.TryGetValue(key, out var value) ? value : null;
    }

    public bool Delete(string key)
    {
        return _store.TryRemove(key, out _);
    }
}