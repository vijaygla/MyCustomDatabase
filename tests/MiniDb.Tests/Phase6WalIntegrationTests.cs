using MiniDb.Engine.Phase3PageStorage;
using MiniDb.Engine.Phase4Indexing;
using MiniDb.Engine.Phase5QueryEngine;
using MiniDb.Engine.Phase6WAL;

namespace MiniDb.Tests;

public class Phase6WalIntegrationTests : IDisposable
{
    private readonly string _testDbPath = "test_wal_int.bin";
    private readonly string _testWalPath = "test_wal_int.wal";

    public Phase6WalIntegrationTests()
    {
        Cleanup();
    }

    [Fact]
    public void ExecutionEngine_ShouldRecoverDataFromWalAfterCrash()
    {
        // 1. Database Session 1: Execute SQL Queries & Write to WAL
        using (var disk = new DiskManager(_testDbPath))
        using (var wal = new WalManager(_testWalPath))
        {
            var buffer = new BufferPoolManager(disk, poolSize: 5);
            var bTree = new BPlusTree(buffer, rootPageId: 0, maxKeys: 3);
            var engine = new ExecutionEngine(bTree, wal);

            engine.Execute("INSERT INTO users VALUES ('1', 'Vijay')");
            engine.Execute("INSERT INTO users VALUES ('2', 'Kumar')");
        }
        // Simulated Sudden Crash (RAM cleared, B+ Tree recreated from scratch)

        // 2. Database Session 2: System Restart with Fresh Engine
        using (var disk = new DiskManager(_testDbPath))
        using (var wal = new WalManager(_testWalPath))
        {
            var buffer = new BufferPoolManager(disk, poolSize: 5);
            var freshBTree = new BPlusTree(buffer, rootPageId: 0, maxKeys: 3);

            // Initialization triggers WAL Crash Recovery automatically
            var engine = new ExecutionEngine(freshBTree, wal);

            // Query using primary key '1' (resolves to composite key 'users:1')
            var result = engine.Execute("SELECT * FROM users WHERE id = 1");
            Assert.Contains("Vijay", result);
        }
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
