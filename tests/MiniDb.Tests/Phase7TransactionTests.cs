using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase5QueryEngine;
using MiniDb.Engine.Phase6WAL;
using Xunit;

namespace MiniDb.Tests;

public class Phase7TransactionTests : IDisposable
{
    private readonly string _testDbPath = "test_p7_tx.bin";
    private readonly string _testWalPath = "test_p7_tx.wal";

    public Phase7TransactionTests()
    {
        Cleanup();
    }

    [Fact]
    public void Transaction_Rollback_ShouldDiscardStagedChanges()
    {
        using var disk = new DiskManager(_testDbPath);
        using var wal = new WalManager(_testWalPath);

        var buffer = new BufferPoolManager(disk, poolSize: 5);
        var bTree = new BPlusTree(buffer, rootPageId: 0, maxKeys: 3);
        var engine = new ExecutionEngine(bTree, wal);

        engine.Execute("CREATE TABLE users (id INT, name TEXT)");
        engine.Execute("BEGIN");
        engine.Execute("INSERT INTO users VALUES ('101', 'TemporaryUser')");

        // Staged insert should be readable inside active transaction
        var inTxSelect = engine.Execute("SELECT * FROM users WHERE id = 101");
        Assert.Contains("TemporaryUser", inTxSelect);

        // Rollback transaction
        var rollbackRes = engine.Execute("ROLLBACK");
        Assert.Contains("rolled back", rollbackRes);

        // Data should NOT exist in B+ Tree Index
        var selectRes = engine.Execute("SELECT * FROM users WHERE id = 101");
        Assert.Contains("(0 rows returned)", selectRes);
    }

    [Fact]
    public void Transaction_Commit_ShouldPersistStagedChanges()
    {
        using var disk = new DiskManager(_testDbPath);
        using var wal = new WalManager(_testWalPath);

        var buffer = new BufferPoolManager(disk, poolSize: 5);
        var bTree = new BPlusTree(buffer, rootPageId: 0, maxKeys: 3);
        var engine = new ExecutionEngine(bTree, wal);

        engine.Execute("CREATE TABLE users (id INT, name TEXT)");
        engine.Execute("BEGIN");
        engine.Execute("INSERT INTO users VALUES ('202', 'PermanentUser')");

        var commitRes = engine.Execute("COMMIT");
        Assert.Contains("committed successfully", commitRes);

        // Data must exist in Persistent Storage
        var selectRes = engine.Execute("SELECT * FROM users WHERE id = 202");
        Assert.Contains("PermanentUser", selectRes);
    }

    private void Cleanup()
    {
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        if (File.Exists(_testWalPath)) File.Delete(_testWalPath);
    }

    public void Dispose()
    {
        Cleanup();
    }
}
