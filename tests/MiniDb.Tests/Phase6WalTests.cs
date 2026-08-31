using MiniDb.Engine.Phase6WAL;

namespace MiniDb.Tests;

public class Phase6WalTests : IDisposable
{
    private readonly string _testWalPath = "test_phase6.wal";

    public Phase6WalTests()
    {
        if (File.Exists(_testWalPath)) File.Delete(_testWalPath);
    }

    [Fact]
    public void WalManager_ShouldWriteAndRecoverRecordsCorrectly()
    {
        // 1. Simulate active database session writing WAL logs
        using (var walWriter = new WalManager(_testWalPath))
        {
            walWriter.WriteRecord(LogRecordType.Insert, "user:101", "Vijay");
            walWriter.WriteRecord(LogRecordType.Insert, "user:102", "Kumar");
            walWriter.WriteRecord(LogRecordType.Delete, "user:101", "");
        }

        // 2. Simulate system crash & restart: Read WAL file back
        using (var walRecovery = new WalManager(_testWalPath))
        {
            List<WalRecord> recoveredLogs = walRecovery.RecoverLogRecords();

            Assert.Equal(3, recoveredLogs.Count);

            Assert.Equal("user:101", recoveredLogs[0].Key);
            Assert.Equal(LogRecordType.Insert, recoveredLogs[0].Type);

            Assert.Equal("user:102", recoveredLogs[1].Key);
            Assert.Equal("Kumar", recoveredLogs[1].Value);

            Assert.Equal(LogRecordType.Delete, recoveredLogs[2].Type);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_testWalPath)) File.Delete(_testWalPath);
    }
}
