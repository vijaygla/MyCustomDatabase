namespace MiniDb.Engine.Phase1InMemory;

public interface IKeyValueStore
{
    void Set(string key, string value);
    string? Get(string key);
    bool Delete(string key);
}
