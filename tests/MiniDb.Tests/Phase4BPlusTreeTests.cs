using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;

namespace MiniDb.Tests;

public class Phase4BPlusTreeTests : IDisposable
{
    private readonly string _testDbPath = "test_phase4.bin";
    private readonly DiskManager _diskManager;
    private readonly BufferPoolManager _bufferPool;
    private readonly BPlusTree _bTree;

    public Phase4BPlusTreeTests()
    {
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        _diskManager = new DiskManager(_testDbPath);
        _bufferPool = new BufferPoolManager(_diskManager, poolSize: 5);
        _bTree = new BPlusTree(_bufferPool, rootPageId: 0, maxKeys: 3);
    }

    [Fact]
    public void BPlusTree_ShouldInsertAndSearchKeys()
    {
        _bTree.Insert("user:1", "Vijay");
        _bTree.Insert("user:2", "Kumar");

        var val1 = _bTree.Search("user:1");
        var val2 = _bTree.Search("user:2");
        var val3 = _bTree.Search("user:99");

        Assert.Equal("Vijay", val1);
        Assert.Equal("Kumar", val2);
        Assert.Null(val3);
    }

    [Fact]
    public void BPlusTree_ShouldHandleNodeSplitsAndGetAllKeys()
    {
        // Insert multiple items to trigger B+ Tree node splitting (maxKeys = 3)
        _bTree.Insert("k1", "v1");
        _bTree.Insert("k2", "v2");
        _bTree.Insert("k3", "v3");
        _bTree.Insert("k4", "v4");
        _bTree.Insert("k5", "v5");

        var allKeys = _bTree.GetAllKeys();

        Assert.Equal(5, allKeys.Count);
        Assert.Equal("v3", _bTree.Search("k3"));
    }

    [Fact]
    public void BPlusTree_ShouldDeleteKeySuccessfully()
    {
        _bTree.Insert("delKey", "delVal");
        Assert.Equal("delVal", _bTree.Search("delKey"));

        bool deleted = _bTree.Delete("delKey");

        Assert.True(deleted);
        Assert.Null(_bTree.Search("delKey"));
    }

    public void Dispose()
    {
        _diskManager.Dispose();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
    }
}
